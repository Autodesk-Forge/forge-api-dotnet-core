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

namespace Autodesk.Forge.Core
{
    /// <summary>
    /// Extensions for adding Forge alternative environment variables to the configuration builder.
    /// </summary>
    public static class ForgeAlternativeConfigurationExtensions
    {
        /// <summary>
        /// Adds Forge alternative environment variables to the configuration builder.
        /// </summary>
        /// <param name="configurationBuilder">The configuration builder.</param>
        /// <returns>The configuration builder with Forge alternative environment variables added.</returns>
        public static IConfigurationBuilder AddForgeAlternativeEnvironmentVariables(this IConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Add(new ForgeAlternativeConfigurationSource());
            return configurationBuilder;
        }

        /// <summary>
        /// Adds APS alternative environment variables to the configuration builder.
        /// </summary>
        /// <param name="configurationBuilder"></param>
        /// <returns></returns>

        public static IConfigurationBuilder AddAPSAlternativeEnvironmentVariables(this IConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Add(new APSAlternativeConfigurationSource());
            return configurationBuilder;
        }

    }

    

    /// <summary>
    /// Represents a configuration source for loading Forge alternative configuration.
    /// </summary>
    public class ForgeAlternativeConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// Builds the Forge alternative configuration provider.
        /// </summary>
        /// <param name="builder">The configuration builder.</param>
        /// <returns>The Forge alternative configuration provider.</returns>
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new ForgeAlternativeConfigurationProvider();
        }
    }

    /// <summary>
    /// Loads the Forge alternative configuration from environment variables.
    /// </summary>
    public class ForgeAlternativeConfigurationProvider : ConfigurationProvider
    {
        /// <summary>
        /// Loads the Forge alternative configuration from environment variables.
        /// </summary>
        public override void Load()
        {
            var id = Environment.GetEnvironmentVariable("FORGE_CLIENT_ID");
            if (!string.IsNullOrEmpty(id))
            {
                this.Data.Add("Forge:ClientId", id);
            }
            var secret = Environment.GetEnvironmentVariable("FORGE_CLIENT_SECRET");
            if (!string.IsNullOrEmpty(secret))
            {
                this.Data.Add("Forge:ClientSecret", secret);
            }
        }
    }



    /// <summary>
    /// Represents a configuration source for loading APS alternative configuration.
    /// </summary>

    public class APSAlternativeConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// Build the APS Environment Configuration Provider
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new APSAlternativeConfigurationProvider();
        }
    }

    /// <summary>
    /// Loads the APS alternative configuration from environment variables.
    /// </summary>

    public class APSAlternativeConfigurationProvider : ConfigurationProvider
    {
        /// <summary>
        /// Loads the APS alternative configuration from environment variables.
        /// </summary>
        public override void Load()
        {
            var id = Environment.GetEnvironmentVariable("APS_CLIENT_ID");
            if (!string.IsNullOrEmpty(id))
            {
                this.Data.Add("Forge:ClientId", id);
            }
            var secret = Environment.GetEnvironmentVariable("APS_CLIENT_SECRET");
            if (!string.IsNullOrEmpty(secret))
            {
                this.Data.Add("Forge:ClientSecret", secret);
            }
        }

    }
}

