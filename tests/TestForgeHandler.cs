using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq.Protected;

namespace Autodesk.Forge.Core.Test
{
    public class TestForgeHandler
    {
        [Fact]
        public void TestNullConfigruationThrows()
        {
            Assert.Throws<ArgumentNullException>(() =>new ForgeHandler(null));
        }

        [Fact]
        public async void TestNoRequestUriThrows()
        {
            var fh = new HttpMessageInvoker(new ForgeHandler(Options.Create(new ForgeConfiguration())));
            await Assert.ThrowsAsync<ArgumentNullException>($"{nameof(HttpRequestMessage)}.{nameof(HttpRequestMessage.RequestUri)}", () => fh.SendAsync(new HttpRequestMessage(), CancellationToken.None));
        }

        [Fact]
        public async void TestNoScopeThrows()
        {
            var fh = new HttpMessageInvoker(new ForgeHandler(Options.Create(new ForgeConfiguration())));
            var req = new HttpRequestMessage();
            req.RequestUri = new Uri("http://example.com");
            await Assert.ThrowsAsync<ArgumentNullException>(ForgeConfiguration.ScopeKey, () => fh.SendAsync(req, CancellationToken.None));
        }

        [Fact]
        public async void TestNoClientIdThrows()
        {
            var fh = new HttpMessageInvoker(new ForgeHandler(Options.Create(new ForgeConfiguration())));
            var req = new HttpRequestMessage();
            req.RequestUri = new Uri("http://example.com");
            req.Properties.Add(ForgeConfiguration.ScopeKey, "somescope");
            await Assert.ThrowsAsync<ArgumentNullException>($"{nameof(ForgeConfiguration)}.{nameof(ForgeConfiguration.ClientId)}", () => fh.SendAsync(req, CancellationToken.None));
        }

        [Fact]
        public async void TestNoClientSecretThrows()
        {
            var fh = new HttpMessageInvoker(new ForgeHandler(Options.Create(new ForgeConfiguration() { ClientId = "ClientId" })));
            var req = new HttpRequestMessage();
            req.RequestUri = new Uri("http://example.com");
            req.Properties.Add(ForgeConfiguration.ScopeKey, "somescope");
            await Assert.ThrowsAsync<ArgumentNullException>($"{nameof(ForgeConfiguration)}.{nameof(ForgeConfiguration.ClientSecret)}", () => fh.SendAsync(req, CancellationToken.None));
        }

        [Fact]
        public async void TestFirstCallAuthenticates()
        {
            var sink = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            sink.Protected().As<HttpMessageInvoker>().SetupSequence(o => o.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage()
                {
                    Content = new StringContent(JsonConvert.SerializeObject(new Dictionary<string, string> { { "token_type", "Bearer" }, { "access_token", "blablabla" }, { "expires_in", "3" } })),
                    StatusCode = System.Net.HttpStatusCode.OK
                })
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK
                });
            var config = new ForgeConfiguration()
            {
                ClientId = "ClientId",
                ClientSecret = "ClientSecret"
            };
            var fh = new HttpMessageInvoker(new ForgeHandler(Options.Create(config))
            {
                InnerHandler = sink.Object
            });

            var req = new HttpRequestMessage();
            req.RequestUri = new Uri("http://example.com");
            req.Properties.Add(ForgeConfiguration.ScopeKey, "somescope");
            await fh.SendAsync(req, CancellationToken.None);

            sink.Protected().As<HttpMessageInvoker>().Verify(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == config.AuthenticationAddress), It.IsAny<CancellationToken>()), Times.Once());
            sink.Protected().As<HttpMessageInvoker>().Verify(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == req.RequestUri), It.IsAny<CancellationToken>()), Times.Once());
        }

        // Make TokenCache public for testing purposes
        class TweakableForgeHandler : ForgeHandler
        {
            public TweakableForgeHandler(IOptions<ForgeConfiguration> configuration)
                :base(configuration)
            {
            }
            public new ITokenCache TokenCache { get { return base.TokenCache; } }
        }
        [Fact]
        public async void TestRetryOnceOnAuthenticationFailure()
        {
            var newToken = "newToken";
            var cachedToken = "cachedToken";
            var req = new HttpRequestMessage();
            req.RequestUri = new Uri("http://example.com");
            var config = new ForgeConfiguration()
            {
                ClientId = "ClientId",
                ClientSecret = "ClientSecret"
            };
            var sink = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            sink.Protected().As<HttpMessageInvoker>().Setup(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == req.RequestUri && r.Headers.Authorization.Parameter == cachedToken), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.Unauthorized,
                    RequestMessage = req
                });
            sink.Protected().As<HttpMessageInvoker>().Setup(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == config.AuthenticationAddress), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new HttpResponseMessage()
                 {
                     Content = new StringContent(JsonConvert.SerializeObject(new Dictionary<string, string> { { "token_type", "Bearer" }, { "access_token", newToken }, { "expires_in", "3" } })),
                     StatusCode = System.Net.HttpStatusCode.OK
                 });
            sink.Protected().As<HttpMessageInvoker>().Setup(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == req.RequestUri && r.Headers.Authorization.Parameter == newToken), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK
                });

            var fh = new TweakableForgeHandler(Options.Create(config))
                {
                    InnerHandler = sink.Object
                };

            var scope = "somescope";

            //we have token but it bad for some reason (maybe revoked)
            fh.TokenCache.Add(scope, $"Bearer {cachedToken}", TimeSpan.FromSeconds(10));

            var invoker = new HttpMessageInvoker(fh);
            
            req.Properties.Add(ForgeConfiguration.ScopeKey, scope);
            await invoker.SendAsync(req, CancellationToken.None);

            sink.VerifyAll();
        }

        [Fact]
        public async void TestRefreshExpiredToken()
        {
            var newToken = "newToken";
            var cachedToken = "cachedToken";
            var req = new HttpRequestMessage();
            req.RequestUri = new Uri("http://example.com");
            var config = new ForgeConfiguration()
            {
                ClientId = "ClientId",
                ClientSecret = "ClientSecret"
            };
            var sink = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            sink.Protected().As<HttpMessageInvoker>().Setup(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == config.AuthenticationAddress), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new HttpResponseMessage()
                 {
                     Content = new StringContent(JsonConvert.SerializeObject(new Dictionary<string, string> { { "token_type", "Bearer" }, { "access_token", newToken }, { "expires_in", "3" } })),
                     StatusCode = System.Net.HttpStatusCode.OK
                 });
            sink.Protected().As<HttpMessageInvoker>().Setup(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == req.RequestUri && r.Headers.Authorization.Parameter == newToken), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK
                });

            var fh = new TweakableForgeHandler(Options.Create(config))
            {
                InnerHandler = sink.Object
            };

            var scope = "somescope";

            //we have token but it is expired already
            fh.TokenCache.Add(scope, $"Bearer {cachedToken}", TimeSpan.FromSeconds(0));

            var invoker = new HttpMessageInvoker(fh);

            req.Properties.Add(ForgeConfiguration.ScopeKey, scope);
            await invoker.SendAsync(req, CancellationToken.None);

            sink.VerifyAll();
        }

        [Fact]
        public async void TestUseGoodToken()
        {
            var cachedToken = "cachedToken";
            var req = new HttpRequestMessage();
            req.RequestUri = new Uri("http://example.com");
            var config = new ForgeConfiguration()
            {
                ClientId = "ClientId",
                ClientSecret = "ClientSecret"
            };
            var sink = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            sink.Protected().As<HttpMessageInvoker>().Setup(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == req.RequestUri && r.Headers.Authorization.Parameter == cachedToken), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK
                });

            var fh = new TweakableForgeHandler(Options.Create(config))
            {
                InnerHandler = sink.Object
            };

            var scope = "somescope";

            //we have token but it is expired already
            fh.TokenCache.Add(scope, $"Bearer {cachedToken}", TimeSpan.FromSeconds(10));

            var invoker = new HttpMessageInvoker(fh);

            req.Properties.Add(ForgeConfiguration.ScopeKey, scope);
            await invoker.SendAsync(req, CancellationToken.None);

            sink.VerifyAll();
        }
    }
}
