/* 
 * Forge SDK
 *
 * The Forge Platform contains an expanding collection of web service components that can be used with Autodesk cloud-based products or your own technologies. Take advantage of Autodesk’s expertise in design and engineering.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Autodesk.Forge.Core
{
    /// <summary>
    /// Extensions for adding ForgeService to the IServiceCollection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures ForgeConfiguration with the given Configuration. It looks for key named "Forge" and uses
        /// the values underneath.
        /// Also adds ForgeService as a typed HttpClient with ForgeHandler as its MessageHandler.
        /// </summary>
        /// <param name="services">The IServiceCollection to add the ForgeService to.</param>
        /// <param name="configuration">The IConfiguration containing the Forge configuration.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        public static IHttpClientBuilder AddForgeService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();
            services.Configure<ForgeConfiguration>(configuration.GetSection("Forge"));
            services.AddTransient<ForgeHandler>();
            return services.AddHttpClient<ForgeService>()
                .AddHttpMessageHandler<ForgeHandler>();
        }


        /// <summary>
        /// Adds the ForgeService to the IServiceCollection with the provided user and configuration.
        /// It configures the ForgeConfiguration using the "Forge" section of the provided configuration.
        /// It also adds the ForgeHandler as a transient service.
        /// Finally, it adds the ForgeService as a typed HttpClient with the ForgeHandler as its MessageHandler.
        /// </summary>
        /// <param name="services">The IServiceCollection to add the ForgeService to.</param>
        /// <param name="user">The user associated with the ForgeService.</param>
        /// <param name="configuration">The IConfiguration containing the Forge configuration.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        public static IHttpClientBuilder AddForgeService(this IServiceCollection services, string user, IConfiguration configuration)
        {
            services.AddOptions();
            services.Configure<ForgeConfiguration>(configuration.GetSection("Forge"));
            services.AddTransient<ForgeHandler>();
            return services.AddHttpClient<ForgeService>(user)
                .AddHttpMessageHandler(() => new ForgeAgentHandler(user))
                .AddHttpMessageHandler<ForgeHandler>();
        }
    }
}
