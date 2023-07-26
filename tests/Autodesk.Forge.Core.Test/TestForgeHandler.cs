using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

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
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(200);
        protected override TimeSpan GetDefaultTimeout()
        {
            return DefaultTimeout;
        }
        protected override (int baseDelayInMs, int multiplier) GetRetryParameters()
        {
            return (5, 10);
        }
    }

    public class TestForgeHandler1
    {
        [Fact]
        public void TestNullConfigurationThrows()
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
            req.Options.Set(ForgeConfiguration.ScopeKey, "somescope");
            await Assert.ThrowsAsync<ArgumentNullException>($"{nameof(ForgeConfiguration)}.{nameof(ForgeConfiguration.ClientId)}", () => fh.SendAsync(req, CancellationToken.None));
        }

        [Fact]
        public async void TestNoClientSecretThrows()
        {
            var fh = new HttpMessageInvoker(new ForgeHandler(Options.Create(new ForgeConfiguration() { ClientId = "ClientId" })));
            var req = new HttpRequestMessage();
            req.RequestUri = new Uri("http://example.com");
            req.Options.Set(ForgeConfiguration.ScopeKey, "somescope");
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
            req.Options.Set(ForgeConfiguration.ScopeKey, "somescope");
            await fh.SendAsync(req, CancellationToken.None);

            sink.Protected().As<HttpMessageInvoker>().Verify(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == config.AuthenticationAddress), It.IsAny<CancellationToken>()), Times.Once());
            sink.Protected().As<HttpMessageInvoker>().Verify(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == req.RequestUri), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async void TestFirstCallAuthenticatesNonDefaultUser()
        {
            var req = new HttpRequestMessage();
            var config = new ForgeConfiguration()
            {
                ClientId = "ClientId",
                ClientSecret = "ClientSecret",
                Agents = new Dictionary<string, ForgeAgentConfiguration>()
                {
                    {
                        "user1", new ForgeAgentConfiguration()
                        {
                            ClientId = "user1-bla",
                            ClientSecret = "user1-blabla"
                        }
                    }
                }
            };
            string actualClientId = null;
            string actualClientSecret = null;
            var sink = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            sink.Protected().As<HttpMessageInvoker>().Setup(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == config.AuthenticationAddress), It.IsAny<CancellationToken>()))
                .Callback<HttpRequestMessage, CancellationToken>((r, ct) =>
                {
                    var clientIdSecret = Encoding.UTF8.GetString(Convert.FromBase64String(r.Headers.Authorization.Parameter)).Split(':');
                    actualClientId = clientIdSecret[0];
                    actualClientSecret = clientIdSecret[1];
                })
                .ReturnsAsync(new HttpResponseMessage()
                {
                    Content = new StringContent(JsonConvert.SerializeObject(new Dictionary<string, string> { { "token_type", "Bearer" }, { "access_token", "blablabla" }, { "expires_in", "3" } })),
                    StatusCode = System.Net.HttpStatusCode.OK
                });
            sink.Protected().As<HttpMessageInvoker>().Setup(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == req.RequestUri), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK
                });
            
            var fh = new HttpMessageInvoker(new ForgeHandler(Options.Create(config))
            {
                InnerHandler = sink.Object
            });

            req.RequestUri = new Uri("http://example.com");
            req.Options.Set(ForgeConfiguration.ScopeKey, "somescope");
            req.Options.Set(ForgeConfiguration.AgentKey, "user1");
            await fh.SendAsync(req, CancellationToken.None);

            Assert.Equal(config.Agents["user1"].ClientId, actualClientId);
            Assert.Equal(config.Agents["user1"].ClientSecret, actualClientSecret);

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

            req.Options.Set(ForgeConfiguration.ScopeKey, scope);
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

            req.Options.Set(ForgeConfiguration.ScopeKey, scope);
            await invoker.SendAsync(req, CancellationToken.None);

            sink.VerifyAll();
        }

        [Fact]
        public async void TestRefreshExpiredTokenByOneThreadOnly()
        {
            var newToken = "newToken";
            var cachedToken = "cachedToken";
            var requestUri = new Uri("http://example.com");
            var config = new ForgeConfiguration()
            {
                ClientId = "ClientId",
                ClientSecret = "ClientSecret"
            };
            var sink = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            sink.Protected().As<HttpMessageInvoker>().Setup(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == config.AuthenticationAddress), It.IsAny<CancellationToken>()))
                 // some artifical delay to ensure that the other thread will attempt to enter the critical section
                 .ReturnsAsync(new HttpResponseMessage()
                 {
                     Content = new StringContent(JsonConvert.SerializeObject(new Dictionary<string, string> { { "token_type", "Bearer" }, { "access_token", newToken }, { "expires_in", "3" } })),
                     StatusCode = System.Net.HttpStatusCode.OK
                 }, TweakableForgeHandler.DefaultTimeout/2
                 );
            sink.Protected().As<HttpMessageInvoker>().Setup(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == requestUri && r.Headers.Authorization.Parameter == newToken), It.IsAny<CancellationToken>()))
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

            //launch 2 threads to make parallel requests
            Func<Task> lambda = async () =>
            {
                var req = new HttpRequestMessage();
                req.RequestUri = requestUri;
                var invoker = new HttpMessageInvoker(fh);

                req.Options.Set(ForgeConfiguration.ScopeKey, scope);
                await invoker.SendAsync(req, CancellationToken.None);
            };

            await Task.WhenAll(lambda(), lambda());

            // We expect exactly one auth call
            sink.Protected().As<HttpMessageInvoker>().Verify(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == config.AuthenticationAddress), It.IsAny<CancellationToken>()), Times.Once());

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

            req.Options.Set(ForgeConfiguration.ScopeKey, scope);
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

            req.Options.Set(ForgeConfiguration.ScopeKey, scope);
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
                .ReturnsAsync(tooManyRequests)
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

            req.Options.Set(ForgeConfiguration.ScopeKey, scope);
            var resp = await invoker.SendAsync(req, CancellationToken.None);

            Assert.Equal(System.Net.HttpStatusCode.GatewayTimeout, resp.StatusCode);

            // We retry 5 times so expect 6 calls 
            sink.Protected().As<HttpMessageInvoker>().Verify(o => o.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(6));
            
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
                .Returns(async (HttpRequestMessage r, CancellationToken ct) =>
                {
                    await Task.Delay(TweakableForgeHandler.DefaultTimeout*2);
                    ct.ThrowIfCancellationRequested();
                    return new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.OK };
                });

            var fh = new TweakableForgeHandler(Options.Create(config))
            {
                InnerHandler = sink.Object
            };

            var scope = "somescope";
            fh.TokenCache.Add(scope, $"Bearer {cachedToken}", TimeSpan.FromSeconds(10));

            var invoker = new HttpMessageInvoker(fh);

            req.Options.Set(ForgeConfiguration.ScopeKey, scope);
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
                    StatusCode = System.Net.HttpStatusCode.InternalServerError
                });

            var fh = new TweakableForgeHandler(Options.Create(config))
            {
                InnerHandler = sink.Object
            };

            var scope = "somescope";
            fh.TokenCache.Add(scope, $"Bearer {cachedToken}", TimeSpan.FromSeconds(10));

            var invoker = new HttpMessageInvoker(fh);

            req.Options.Set(ForgeConfiguration.ScopeKey, scope);
            // We tolerate 3 failures before we break the circuit
            for (int i = 0; i < 3; i++)
            {
                var resp = await invoker.SendAsync(req, CancellationToken.None);
                Assert.Equal(System.Net.HttpStatusCode.InternalServerError, resp.StatusCode);
            }

            await Assert.ThrowsAsync<Polly.CircuitBreaker.BrokenCircuitException<HttpResponseMessage>>(async () => await invoker.SendAsync(req, CancellationToken.None));

            
            sink.Protected().As<HttpMessageInvoker>().Verify(o => o.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

            sink.VerifyAll();
        }
    }

    /// <summary>
    /// Unit tests for custom timeout.
    /// </summary>
    public class TestCustomTimeout
    {
        private const string CachedToken = "cachedToken";
        private const string Scope = "somescope";

        private readonly ForgeConfiguration _forgeConfig = new ForgeConfiguration
                                                            {
                                                                ClientId = "ClientId",
                                                                ClientSecret = "ClientSecret"
                                                            };

        [Fact]
        public async void TestTriggeredTimeout()
        {
            var (sink, requestSender) = GetReady(1, TimeSpan.FromMilliseconds(1100));
            await Assert.ThrowsAsync<Polly.Timeout.TimeoutRejectedException>(async () => await requestSender());

            sink.VerifyAll();
        }

        [Fact]
        public async void TestNoTimeout()
        {
            var (sink, requestSender) = GetReady(1, TimeSpan.FromMilliseconds(100));

            HttpResponseMessage response = await requestSender();
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            sink.VerifyAll();
        }

        /// <summary>
        /// Create all required components for custom timeout validation.
        /// </summary>
        /// <param name="allowedTimeInSec">Allowed time in seconds.</param>
        /// <param name="responseTime">Actual response time.</param>
        /// <returns>
        /// Tuple with:
        /// * mock to validate after tests are complete.
        /// * functor to perform mocked HTTP request/response operation.
        /// </returns>
        private (Mock<HttpMessageHandler> sink, Func<Task<HttpResponseMessage>> requestSender) GetReady(int allowedTimeInSec, TimeSpan responseTime)
        {
            var req = RequestWithTimeout(allowedTimeInSec);
            var sink = MakeSink(req, responseTime);

            var fh = new TweakableForgeHandler(Options.Create(_forgeConfig))
            {
                InnerHandler = sink.Object
            };
            fh.TokenCache.Add(Scope, $"Bearer {CachedToken}", TimeSpan.FromSeconds(10));

            var invoker = new HttpMessageInvoker(fh);
            return (sink, () => invoker.SendAsync(req, new CancellationToken()));
        }

        /// <summary>
        /// Create mocked HTTP message handler, who emulates timeout.
        /// </summary>
        /// <param name="req">Expected HTTP request.</param>
        /// <param name="responseTimeout">Response timeout in seconds.</param>
        private static Mock<HttpMessageHandler> MakeSink(HttpRequestMessage req, TimeSpan responseTime)
        {
            var sink = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            sink.Protected()
                .As<HttpMessageInvoker>()
                .Setup(o => o.SendAsync(It.Is(EnsureRequest(req)), It.IsAny<CancellationToken>()))
                .Returns(async (HttpRequestMessage r, CancellationToken ct) =>
                {
                    await Task.Delay(responseTime);
                    ct.ThrowIfCancellationRequested();
                    return new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.OK };
                });

            return sink;
        }

        private static Expression<Func<HttpRequestMessage, bool>> EnsureRequest(HttpRequestMessage expected)
        {
            return (HttpRequestMessage actual) => (actual.RequestUri == expected.RequestUri) &&
                                                  (actual.Headers.Authorization.Parameter == CachedToken);
        }

        /// <summary>
        /// Create HTTP request message with custom timeout.
        /// </summary>
        /// <param name="timeout">Timeout in seconds.</param>
        private static HttpRequestMessage RequestWithTimeout(int timeout)
        {
            var req =  new HttpRequestMessage
                    {
                        RequestUri = new Uri("http://example.com")
                    };

            req.Options.Set(ForgeConfiguration.ScopeKey, Scope);
            req.Options.Set(ForgeConfiguration.TimeoutKey, timeout);

            return req;
        }
    }

}
