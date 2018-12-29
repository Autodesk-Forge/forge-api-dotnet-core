using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace Autodesk.Forge.Core
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureForge(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();
            return services.Configure<ForgeConfiguration>(configuration.GetSection("Forge"));
        }
        public static IHttpClientBuilder AddForgeService(this IServiceCollection services)
        {
            services.AddTransient<ForgeHandler>();
            return services.AddHttpClient<ForgeService>()
                .AddHttpMessageHandler<ForgeHandler>();
        }
    }
}
