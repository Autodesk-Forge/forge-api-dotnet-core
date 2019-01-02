using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
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
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();
            services.AddForgeService(configuration);
            var serviceProvider = services.BuildServiceProvider();

            var config = serviceProvider.GetRequiredService<IOptions<ForgeConfiguration>>();
            Assert.Equal("bla", config.Value.ClientId);
        }
    }
}
