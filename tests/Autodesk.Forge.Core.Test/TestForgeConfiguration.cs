using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text;
using Xunit;

namespace Autodesk.Forge.Core.Test
{
    public class TestForgeConfiguration
    {
        [Fact]
        public void TestDefault()
        {
            var config = new ForgeConfiguration();
            Assert.NotNull(config.AuthenticationAddress);
        }

        [Fact]
        public void TestValuesFromEnvironment()
        {
            Environment.SetEnvironmentVariable("Forge__ClientId", "bla");
            Environment.SetEnvironmentVariable("Forge__ClientSecret", "blabla");
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

        [Fact]
        public void TestValuesFromLegacyEnvironment()
        {
            Environment.SetEnvironmentVariable("FORGE_CLIENT_ID", "bla");
            Environment.SetEnvironmentVariable("FORGE_CLIENT_SECRET", "blabla");
            var configuration = new ConfigurationBuilder()
                .AddForgeAlternativeEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();
            services.AddForgeService(configuration);
            var serviceProvider = services.BuildServiceProvider();

            var config = serviceProvider.GetRequiredService<IOptions<ForgeConfiguration>>();
            Assert.Equal("bla", config.Value.ClientId);
            Assert.Equal("blabla", config.Value.ClientSecret);
        }

        [Fact]
        public void TestValuesFromJson()
        {
            var json = @"
            {
                ""Forge"" : {
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

        [Fact]
        public void TestValuesFromJsonMoreAgents()
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

            var services = new ServiceCollection();
            services.AddForgeService(configuration);
            var serviceProvider = services.BuildServiceProvider();

            var config = serviceProvider.GetRequiredService<IOptions<ForgeConfiguration>>();
            Assert.Equal("bla", config.Value.ClientId);
            Assert.Equal("blabla", config.Value.ClientSecret);
            Assert.Equal("user1-bla", config.Value.Agents["user1"].ClientId);
            Assert.Equal("user1-blabla", config.Value.Agents["user1"].ClientSecret);
        }
    }
}
