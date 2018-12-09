using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net.Http;

namespace Autodesk.Forge.Core
{
    public class ForgeService
    {
        public ForgeService(HttpClient client)
        {
            this.Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public HttpClient Client { get; private set; }

        public static ForgeService CreateDefault()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();
            services.ConfigureForge(configuration);
            services.AddForgeService();
            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider.GetRequiredService<ForgeService>();
        }
    }
}
