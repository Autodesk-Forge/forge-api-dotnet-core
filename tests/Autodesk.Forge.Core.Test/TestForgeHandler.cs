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
using Xunit.Abstractions;

namespace Autodesk.Forge.Core.Test
{
    // Make TokenCache public for testing purposes
    class TweakableForgeHandler : ForgeHandler
    {
        public TweakableForgeHandler(IOptions<ForgeConfiguration> configuration)
            : base(configuration)
        {
        }
        public new ITokenCache TokenCache { get { return base.TokenCache; } }
    }

    public class TestForgeHandler1
    {
        [Fact]
        public void TestNullConfigruationThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new ForgeHandler(null));
        }

        [Fact]
        public async void TestNoRequestUriThrows()
        {
            var fh = new HttpMessageInvoker(new ForgeHandler(Options.Create(new ForgeConfiguration())));
            await Assert.ThrowsAsync<ArgumentNullException>($"{nameof(HttpRequestMessage)}.{nameof(HttpRequestMessage.RequestUri)}", () => fh.SendAsync(new HttpRequestMessage(), CancellationToken.None));
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
            fh.TokenCache.Add(scope, $"Bearer {cachedToken}", TimeSpan.FromSeconds(300));

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
            fh.TokenCache.Add(scope, $"Bearer {cachedToken}", TimeSpan.FromSeconds(10));

            var invoker = new HttpMessageInvoker(fh);

            req.Properties.Add(ForgeConfiguration.ScopeKey, scope);
            var resp = await invoker.SendAsync(req, CancellationToken.None);

            Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);

            // We expect exactly one network call
            sink.Protected().As<HttpMessageInvoker>().Verify(o => o.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once());

            sink.VerifyAll();
        }

        [Fact]
        public async void TestNoRefreshOnClientProvidedToken()
        {
            var token = "blabla";
            var req = new HttpRequestMessage();
            req.RequestUri = new Uri("http://example.com");
            var config = new ForgeConfiguration()
            {
                ClientId = "ClientId",
                ClientSecret = "ClientSecret"
            };
            var sink = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            sink.Protected().As<HttpMessageInvoker>().Setup(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == req.RequestUri && r.Headers.Authorization.Parameter == token), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.Unauthorized
                });

            var fh = new TweakableForgeHandler(Options.Create(config))
            {
                InnerHandler = sink.Object
            };

            var scope = "somescope";

            var invoker = new HttpMessageInvoker(fh);

            req.Properties.Add(ForgeConfiguration.ScopeKey, scope);
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var resp = await invoker.SendAsync(req, CancellationToken.None);

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, resp.StatusCode);

            // We expect exactly one network call
            sink.Protected().As<HttpMessageInvoker>().Verify(o => o.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once());

            sink.VerifyAll();
        }
    }

    // put time consuming tests into separate classes so they are executed in parallel
    public class TestForgeHandler2
    {
        [Fact]
        public async void TestCorrectNumberOfRetries()
        {
            var cachedToken = "cachedToken";
            var req = new HttpRequestMessage();
            req.RequestUri = new Uri("http://example.com");
            var config = new ForgeConfiguration()
            {
                ClientId = "ClientId",
                ClientSecret = "ClientSecret"
            };

            var gatewayTimeout = new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.GatewayTimeout };
            var tooManyRequests = new HttpResponseMessage { StatusCode = (System.Net.HttpStatusCode)429 };
            tooManyRequests.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(2));
            var sink = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            sink.Protected().As<HttpMessageInvoker>().SetupSequence(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == req.RequestUri && r.Headers.Authorization.Parameter == cachedToken), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tooManyRequests)
                .ThrowsAsync(new HttpRequestException())
                .ReturnsAsync(gatewayTimeout)
                .ReturnsAsync(gatewayTimeout);
                

            var fh = new TweakableForgeHandler(Options.Create(config))
            {
                InnerHandler = sink.Object
            };

            var scope = "somescope";
            fh.TokenCache.Add(scope, $"Bearer {cachedToken}", TimeSpan.FromSeconds(10));

            var invoker = new HttpMessageInvoker(fh);

            req.Properties.Add(ForgeConfiguration.ScopeKey, scope);
            var resp = await invoker.SendAsync(req, CancellationToken.None);

            Assert.Equal(System.Net.HttpStatusCode.GatewayTimeout, resp.StatusCode);

            // We retry 3 times so expect 4 calls 
            sink.Protected().As<HttpMessageInvoker>().Verify(o => o.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
            
            sink.VerifyAll();
        }
    }

    public class TestForgeHandler3
    {
        [Fact]
        public async void TestTimeout()
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
                    StatusCode = System.Net.HttpStatusCode.GatewayTimeout
                }, TimeSpan.FromSeconds(12));

            var fh = new TweakableForgeHandler(Options.Create(config))
            {
                InnerHandler = sink.Object
            };

            var scope = "somescope";
            fh.TokenCache.Add(scope, $"Bearer {cachedToken}", TimeSpan.FromSeconds(10));

            var invoker = new HttpMessageInvoker(fh);

            req.Properties.Add(ForgeConfiguration.ScopeKey, scope);
            await Assert.ThrowsAsync<Polly.Timeout.TimeoutRejectedException>(async () => await invoker.SendAsync(req, new CancellationToken()));

            sink.VerifyAll();
        }
    }

    public class TestForgeHandler4
    { 
        [Fact]
        public async void TestCircuitBreaker()
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
                    StatusCode = System.Net.HttpStatusCode.GatewayTimeout
                });

            var fh = new TweakableForgeHandler(Options.Create(config))
            {
                InnerHandler = sink.Object
            };

            var scope = "somescope";
            fh.TokenCache.Add(scope, $"Bearer {cachedToken}", TimeSpan.FromSeconds(10));

            var invoker = new HttpMessageInvoker(fh);

            req.Properties.Add(ForgeConfiguration.ScopeKey, scope);
            await invoker.SendAsync(req, CancellationToken.None);

            await Assert.ThrowsAsync<Polly.CircuitBreaker.BrokenCircuitException<HttpResponseMessage>>(async () => await invoker.SendAsync(req, CancellationToken.None));

            // We tolerate 5 failures before we break the circuit
            sink.Protected().As<HttpMessageInvoker>().Verify(o => o.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(5));

            sink.VerifyAll();
        }
    }
}
