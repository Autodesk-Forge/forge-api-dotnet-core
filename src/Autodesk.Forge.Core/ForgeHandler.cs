﻿/* 
 * Forge SDK
 *
 * The Forge Platform contains an expanding collection of web service components that can be used with Autodesk cloud-based products or your own technologies. Take advantage of Autodesk’s expertise in design and engineering.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Autodesk.Forge.Core
{
    public class ForgeHandler : DelegatingHandler
    {
        private static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private readonly Random rand = new Random();
        private readonly IAsyncPolicy<HttpResponseMessage> resiliencyPolicies;
        protected readonly IOptions<ForgeConfiguration> configuration;

        protected ITokenCache TokenCache { get; private set; }

        public ForgeHandler(IOptions<ForgeConfiguration> configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.TokenCache = new TokenCache();
            this.resiliencyPolicies = GetResiliencyPolicies(GetDefaultTimeout());
        }
        protected virtual TimeSpan GetDefaultTimeout()
        {
             // use timeout greater than the forge gateways (10s), we handle the GatewayTimeout response
            return TimeSpan.FromSeconds(15); 
        }
        protected virtual (int baseDelayInMs, int multiplier ) GetRetryParameters()
        {
            return (500, 1000);
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri == null)
            {
                throw new ArgumentNullException($"{nameof(HttpRequestMessage)}.{nameof(HttpRequestMessage.RequestUri)}");
            }

            IAsyncPolicy<HttpResponseMessage> policies;

            // check if request wants custom timeout
            if (request.Options.TryGetValue(ForgeConfiguration.TimeoutKey, out var timeoutValue))
            {
                policies = GetResiliencyPolicies(TimeSpan.FromSeconds(timeoutValue));
            }
            else
            {
                policies = this.resiliencyPolicies;
            }


            if (request.Headers.Authorization == null &&
                request.Options.TryGetValue(ForgeConfiguration.ScopeKey, out _))
            {
                // no authorization header so we manage authorization
                await RefreshTokenAsync(request, false, cancellationToken);
                // add a retry policy so that we refresh invalid tokens
                policies = policies.WrapAsync(GetTokenRefreshPolicy());
            }
            return await policies.ExecuteAsync(async (ct) => await base.SendAsync(request, ct), cancellationToken);
        }

        protected virtual IAsyncPolicy<HttpResponseMessage> GetTokenRefreshPolicy()
        {
            // A policy that attempts to retry exactly once when 401 error is received after obtaining a new token
            return Policy
                .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.Unauthorized)
                .RetryAsync(
                    retryCount: 1,
                    onRetryAsync: async (outcome, retryNumber, context) => await RefreshTokenAsync(outcome.Result.RequestMessage, true, CancellationToken.None)
                );
        }

        protected virtual IAsyncPolicy<HttpResponseMessage> GetResiliencyPolicies(TimeSpan timeoutValue)
        {
            // Retry when HttpRequestException is thrown (low level network error) or 
            // the server returns an error code that we think is transient
            //
            int[] retriable = {
                        (int)HttpStatusCode.RequestTimeout, // 408
                        429, //too many requests
                        (int)HttpStatusCode.BadGateway, // 502
                        (int)HttpStatusCode.ServiceUnavailable, // 503
                        (int)HttpStatusCode.GatewayTimeout // 504
                        };
            var (retryBaseDelay, retryMultiplier) = GetRetryParameters();
            var retry = Policy
                .Handle<HttpRequestException>()
                .Or<Polly.Timeout.TimeoutRejectedException>()// thrown by Polly's TimeoutPolicy if the inner call times out
                .OrResult<HttpResponseMessage>(response =>
                {
                    return retriable.Contains((int)response.StatusCode);
                })
                .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: (retryCount, response, context) =>
                {
                    // First see how long the server wants us to wait
                    var serverWait = response.Result?.Headers.RetryAfter?.Delta;
                    // Calculate how long we want to wait in milliseconds
                    var clientWait = (double)rand.Next(retryBaseDelay /*500*/, (int)Math.Pow(2, retryCount) * retryMultiplier /*1000*/);
                    var wait = clientWait;
                    if (serverWait.HasValue)
                    {
                        wait = serverWait.Value.TotalMilliseconds + clientWait;
                    }
                    return TimeSpan.FromMilliseconds(wait);
                },
                onRetryAsync: (response, sleepTime, retryCount, content) => Task.CompletedTask);

            // break circuit after 3 errors and keep it broken for 1 minute
            var breaker = Policy
                .Handle<HttpRequestException>()
                .Or<Polly.Timeout.TimeoutRejectedException>()// thrown by Polly's TimeoutPolicy if the inner call times out
                .OrResult<HttpResponseMessage>(response =>
                {
                    //we want to break the circuit if retriable errors persist or internal errors from the server
                    return retriable.Contains((int)response.StatusCode) || 
                           response.StatusCode == HttpStatusCode.InternalServerError;
                })
                .CircuitBreakerAsync(3, TimeSpan.FromMinutes(1));

            // timeout handler
            // https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory#use-case-applying-timeouts
            var timeout = Policy.TimeoutAsync<HttpResponseMessage>(timeoutValue);

            // ordering is important here!
            return Policy.WrapAsync<HttpResponseMessage>(breaker, retry, timeout);
        }

        protected virtual async Task RefreshTokenAsync(HttpRequestMessage request, bool ignoreCache, CancellationToken cancellationToken)
        {
            if (request.Options.TryGetValue(ForgeConfiguration.ScopeKey, out var scope))
            {
                var user = string.Empty;
                request.Options.TryGetValue(ForgeConfiguration.UserKey, out user);
                var cacheKey = user + scope;
                // it is possible that multiple threads get here at the same time, only one of them should 
                // attempt to refresh the token. 
                // NOTE: We could use different semaphores for different cacheKey here. It is a minor optimization.
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    if (ignoreCache || !TokenCache.TryGetValue(cacheKey, out var token))
                    {
                        TimeSpan expiry;
                        (token, expiry) = await this.Get2LeggedTokenAsync(user, scope, cancellationToken);
                        TokenCache.Add(cacheKey, token, expiry);
                    }
                    request.Headers.Authorization = AuthenticationHeaderValue.Parse(token);
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }
        protected virtual async Task<(string, TimeSpan)> Get2LeggedTokenAsync(string user, string scope, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage())
            {
                var config = this.configuration.Value;
                var clientId = string.IsNullOrEmpty(user) ? config.ClientId : config.Users[user].ClientId;
                if (string.IsNullOrEmpty(clientId))
                {
                    throw new ArgumentNullException($"{nameof(ForgeConfiguration)}.{nameof(ForgeConfiguration.ClientId)}");
                }
                var clientSecret = string.IsNullOrEmpty(user) ? config.ClientSecret : config.Users[user].ClientSecret;
                if (string.IsNullOrEmpty(clientSecret))
                {
                    throw new ArgumentNullException($"{nameof(ForgeConfiguration)}.{nameof(ForgeConfiguration.ClientSecret)}");
                }
                var values = new List<KeyValuePair<string, string>>();
                values.Add(new KeyValuePair<string, string>("client_id", clientId));
                values.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
                values.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
                values.Add(new KeyValuePair<string, string>("scope", scope));
                request.Content = new FormUrlEncodedContent(values);
                request.RequestUri = config.AuthenticationAddress;
                request.Method = HttpMethod.Post;

                var response = await this.resiliencyPolicies.ExecuteAsync(async () => await base.SendAsync(request, cancellationToken));

                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var resValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
                return (resValues["token_type"] + " " + resValues["access_token"], TimeSpan.FromSeconds(double.Parse(resValues["expires_in"])));
            }
        }
    }
}
