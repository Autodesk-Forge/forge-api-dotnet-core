using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace Autodesk.Forge.Core
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures ForgeConfiguration with the given Configuration. It looks for key named "Forge" and uses
        /// the values underneath.
        /// Also adds ForgeService as a typed HttpClient with ForgeHandler as its MessageHandler.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IHttpClientBuilder AddForgeService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();
            services.Configure<ForgeConfiguration>(configuration.GetSection("Forge"));
            services.AddTransient<ForgeHandler>();
            return services.AddHttpClient<ForgeService>()
                .AddHttpMessageHandler<ForgeHandler>();
        }
    }
}
