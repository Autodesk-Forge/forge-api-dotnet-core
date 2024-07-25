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
    /// Represents a service for interacting with the Autodesk Forge platform.
    /// </summary>
    public class ForgeService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ForgeService"/> class with the specified <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="client">The <see cref="HttpClient"/> instance to be used for making HTTP requests.</param>
        public ForgeService(HttpClient client)
        {
            this.Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Gets the <see cref="HttpClient"/> instance used by the Forge service.
        /// </summary>
        public HttpClient Client { get; private set; }

        /// <summary>
        /// Creates a default instance of the <see cref="ForgeService"/> class.
        /// </summary>
        /// <returns>A default instance of the <see cref="ForgeService"/> class.</returns>
        public static ForgeService CreateDefault()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();
            services.AddForgeService(configuration);
            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider.GetRequiredService<ForgeService>();
        }
    }
}
