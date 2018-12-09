using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace Autodesk.Forge.Core
{
    public static class ServiceCollectionExtensions
    {
        public static void AddForgeService(this IServiceCollection services, IConfiguration appConfiguration)
        {
            services.AddOptions();
            services.AddTransient<ForgeHandler>();
            services.Configure<ForgeConfiguration>(appConfiguration.GetSection("Forge"));
            services.AddHttpClient<ForgeService>()
                .AddHttpMessageHandler<ForgeHandler>();
        }
    }
}
