using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Autodesk.Forge.Core.Test
{
    public class TestAPSConfiguration
    {
        /// <summary>
        ///  Tests using APS__ClientId and APS__ClientSecret environment variables dotnet core style
        /// </summary>
        [Fact]
        public void TestAPSConfigFromEnvironmentVariables_DoubleUnderscoreFormat()
        {
            Environment.SetEnvironmentVariable("APS__ClientId", "bla");
            Environment.SetEnvironmentVariable("APS__ClientSecret", "blabla");
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();
            services.AddForgeService(configuration);
            var serviceProvider = services.BuildServiceProvider();

            var config = serviceProvider.GetRequiredService<IOptions<ForgeConfiguration>>();
            Assert.Equal("bla", config.Value.ClientId);
            Assert.Equal("blabla", config.Value.ClientSecret);
        }

        /// <summary>
        /// Tests using APS_CLIENT_ID and APS_CLIENT_SECRET environment variables
        /// </summary>

        [Fact]
        public void TestAPSConfigFromEnvironmentVariables_UnderscoreFormat()
        {
            Environment.SetEnvironmentVariable("APS_CLIENT_ID", "bla");
            Environment.SetEnvironmentVariable("APS_CLIENT_SECRET", "blabla");
            var configuration = new ConfigurationBuilder()
                .AddAPSAlternativeEnvironmentVariables()
                .Build();
            var services = new ServiceCollection();
            services.AddForgeService(configuration);
            var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredService<IOptions<ForgeConfiguration>>();
            Assert.Equal("bla", config.Value.ClientId);
            Assert.Equal("blabla", config.Value.ClientSecret);

        }
        /// <summary>
        /// Tests loading APS configuration values from JSON with ClientId and ClientSecret
        /// </summary>

        [Fact]
        public void TestAPSConfigFromJson()
        {
            var json = @"
            {
                ""APS"" : {
                    ""ClientId"" : ""bla"",
                    ""ClientSecret"" : ""blabla""
                }
            }";
            var configuration = new ConfigurationBuilder()
                .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
                .Build();

            var services = new ServiceCollection();
            services.AddForgeService(configuration);
            var serviceProvider = services.BuildServiceProvider();

            var config = serviceProvider.GetRequiredService<IOptions<ForgeConfiguration>>();
            Assert.Equal("bla", config.Value.ClientId);
            Assert.Equal("blabla", config.Value.ClientSecret);
        }

        /// <summary>
        /// Tests loading APS configuration values from JSON with additional agent configurations
        /// </summary>

        [Fact]
        public void TestAPSConfigFromJsonWithAgents()
        {
            var json = @"
            {
                ""APS"" : {
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

            var services = new ServiceCollection();
            services.AddForgeService(configuration);
            var serviceProvider = services.BuildServiceProvider();

            var config = serviceProvider.GetRequiredService<IOptions<ForgeConfiguration>>();
            Assert.Equal("bla", config.Value.ClientId);
            Assert.Equal("blabla", config.Value.ClientSecret);
            Assert.Equal("user1-bla", config.Value.Agents["user1"].ClientId);
            Assert.Equal("user1-blabla", config.Value.Agents["user1"].ClientSecret);
        }

        /// <summary>  
        /// Tests APS configuration for user agent "user1" and checks proper handling of authentication and request headers.
        /// </summary>
        [Fact]
        public async Task TestAPSUserAgent()
        {
            var json = @"
            {
                ""APS"" : {
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
