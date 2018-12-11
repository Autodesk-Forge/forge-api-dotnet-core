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

namespace Autodesk.Forge.Core
{
    public class ForgeHandler : DelegatingHandler
    {
        protected readonly IOptions<ForgeConfiguration> configuration;

        protected Dictionary<string, string> TokenCache { get; private set; } = new Dictionary<string, string>();

        public ForgeHandler(IOptions<ForgeConfiguration> configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri == null)
            {
                throw new ArgumentNullException($"{nameof(HttpRequestMessage)}.{nameof(HttpRequestMessage.RequestUri)}");
            }
            await RefreshTokenAsync(request, false, cancellationToken);
            return await GetResiliencyPolicy().WrapAsync(GetTokenRefreshPolicy()).ExecuteAsync(async () => await base.SendAsync(request, cancellationToken));
        }
        protected virtual IAsyncPolicy<HttpResponseMessage> GetTokenRefreshPolicy()
        {
            // A policy that attempt to retry exactly once when 401 error is received after obtaining a new token
            return Policy
                .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.Unauthorized)
                .RetryAsync(
                    retryCount: 1,
                    onRetryAsync: async (outcome, retryNumber, context) => await RefreshTokenAsync(outcome.Result.RequestMessage, true, CancellationToken.None)
                );
        }
        protected virtual IAsyncPolicy<HttpResponseMessage> GetResiliencyPolicy()
        {
            // We probably want to do something more sophisticated here in the long run
            // e.g. add a circuit breaker, jitter to the retry, other status codes to retry etc.
            return Policy
                .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.GatewayTimeout)
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15),
                    TimeSpan.FromSeconds(30)
                });
        }
        protected virtual async Task RefreshTokenAsync(HttpRequestMessage request, bool ignoreCache, CancellationToken cancellationToken)
        {
            if (request.Properties.TryGetValue(ForgeConfiguration.ScopeKey, out var obj) && obj != null && obj is string)
            {
                var scope = (string)obj;
                if (ignoreCache || !TokenCache.TryGetValue(scope, out var token))
                {
                    token = await this.Get2LeggedTokenAsync(scope, cancellationToken);
                    TokenCache[scope] = token;
                }
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(token);
            }
            else
            {
                throw new ArgumentNullException(ForgeConfiguration.ScopeKey, "The incoming HttpRequestMessage does not have a scope property. Use request.Properties.Add(ForgeConfiguration.ScopeKey, <scopes>)");
            }
        }
        protected virtual async Task<string> Get2LeggedTokenAsync(string scope, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage())
            {
                var config = this.configuration.Value;
                if (string.IsNullOrEmpty(config.ClientId))
                {
                    throw new ArgumentNullException($"{nameof(ForgeConfiguration)}.{nameof(ForgeConfiguration.ClientId)}");
                }
                if (string.IsNullOrEmpty(config.ClientSecret))
                {
                    throw new ArgumentNullException($"{nameof(ForgeConfiguration)}.{nameof(ForgeConfiguration.ClientSecret)}");
                }
                var values = new List<KeyValuePair<string, string>>();
                values.Add(new KeyValuePair<string, string>("client_id", config.ClientId));
                values.Add(new KeyValuePair<string, string>("client_secret", config.ClientSecret));
                values.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
                values.Add(new KeyValuePair<string, string>("scope", scope));
                request.Content = new FormUrlEncodedContent(values);
                request.RequestUri = config.AuthenticationAddress;
                request.Method = HttpMethod.Post;

                var response = await GetResiliencyPolicy().ExecuteAsync(async () => await base.SendAsync(request, cancellationToken));

                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var resValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
                return resValues["token_type"] + " " + resValues["access_token"];
            }
        }
    }
}
