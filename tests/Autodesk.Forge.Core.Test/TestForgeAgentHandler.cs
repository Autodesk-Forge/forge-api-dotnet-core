using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System.Text;
using Xunit;

namespace Autodesk.Forge.Core.Test
{
    public class TestForgeAgentHandler
    {
        [Fact]
        public async Task TestUser()
        {
            var json = @"
            {
                ""Forge"" : {
                    ""ClientId"" : ""bla"",
                    ""ClientSecret"" : ""blabla"",
                    ""Agents"" : {
                        ""user1"" : {
                            ""ClientId"" : ""user1-bla"",
                            ""ClientSecret"" : ""user1-blabla""
                        }
                    }
                }
            }";
            var configuration = new ConfigurationBuilder()
                .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
                .Build();

            var sink = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var services = new ServiceCollection();
            services.AddForgeService("user1", configuration).ConfigurePrimaryHttpMessageHandler(() => sink.Object);
            var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredService<IOptions<ForgeConfiguration>>().Value;
            var req = new HttpRequestMessage();
            req.RequestUri = new Uri("http://example.com");
            req.Options.Set(ForgeConfiguration.ScopeKey, "somescope");

            string user = null;
            sink.Protected().As<HttpMessageInvoker>().Setup(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == config.AuthenticationAddress), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage()
                {
                    Content = new StringContent(JsonConvert.SerializeObject(new Dictionary<string, string> { { "token_type", "Bearer" }, { "access_token", "blablabla" }, { "expires_in", "3" } })),
                    StatusCode = System.Net.HttpStatusCode.OK
                });
            sink.Protected().As<HttpMessageInvoker>().Setup(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == req.RequestUri), It.IsAny<CancellationToken>()))
                .Callback<HttpRequestMessage, CancellationToken>((r, ct) =>
                {
                    r.Options.TryGetValue(ForgeConfiguration.AgentKey, out user);
                })
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK
                });


            var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = clientFactory.CreateClient("user1");
            var resp = await client.SendAsync(req, CancellationToken.None);

            sink.Protected().As<HttpMessageInvoker>().Verify(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == config.AuthenticationAddress), It.IsAny<CancellationToken>()), Times.Once());
            sink.Protected().As<HttpMessageInvoker>().Verify(o => o.SendAsync(It.Is<HttpRequestMessage>(r => r.RequestUri == req.RequestUri), It.IsAny<CancellationToken>()), Times.Once());
            Assert.Equal("user1", user);
        }
    }
}
