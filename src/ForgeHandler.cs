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
        public readonly IOptions<ForgeConfiguration> configuration;

        private Dictionary<string, string> tokenCache = new Dictionary<string, string>();

        public ForgeHandler(IOptions<ForgeConfiguration> configuration)
        {
            this.configuration = configuration;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await RefreshTokenAsync(request, false, cancellationToken);
            return await Policy
                .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.Unauthorized)
                .RetryAsync(
                    retryCount: 1,
                    onRetryAsync: async (outcome, retryNumber, context) => await RefreshTokenAsync(request, true, CancellationToken.None)
                )
                .WrapAsync( Policy
                .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.GatewayTimeout)
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15),
                    TimeSpan.FromSeconds(30)
                }))
                .ExecuteAsync(async () => await base.SendAsync(request, cancellationToken));
        }

        private async Task RefreshTokenAsync(HttpRequestMessage request, bool ignoreCache, CancellationToken cancellationToken)
        {
            if (request.Properties.TryGetValue(Core.ForgeConfiguration.ScopeKey, out var obj) && obj != null && obj is string)
            {
                var scope = (string)obj;
                if (ignoreCache || !tokenCache.TryGetValue(scope, out var token))
                {
                    token = await this.Get2LeggedTokenAsync(scope, cancellationToken);
                    tokenCache[scope] = token;
                }
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(token);
            }
        }
        private async Task<string> Get2LeggedTokenAsync(string scope, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage())
            {
                var config = this.configuration.Value;
                var values = new List<KeyValuePair<string, string>>();
                values.Add(new KeyValuePair<string, string>("client_id", config.ClientId));
                values.Add(new KeyValuePair<string, string>("client_secret", config.ClientSecret));
                values.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
                values.Add(new KeyValuePair<string, string>("scope", scope));
                request.Content = new FormUrlEncodedContent(values);
                request.RequestUri = config.AuthenticationAddress;
                request.Method = HttpMethod.Post;

                var response = await base.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var resValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
                return resValues["token_type"] + " " + resValues["access_token"];
            }
        }
    }
}
