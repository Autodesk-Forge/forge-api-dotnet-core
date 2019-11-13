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
using System;

namespace Autodesk.Forge.Core
{
    public static class ForgeAlternativeConfigurationExtensions
    {
        public static IConfigurationBuilder AddForgeAlternativeEnvironmentVariables(this IConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Add(new ForgeAlternativeConfigurationSource());
            return configurationBuilder;
        }
    }

    public class ForgeAlternativeConfigurationSource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new ForgeAlternativeConfigurationProvider();
        }
    }

    public class ForgeAlternativeConfigurationProvider : ConfigurationProvider
    {
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
            
            var proxyUrl = Environment.GetEnvironmentVariable("FORGE_PROXY_URL");
            if (!string.IsNullOrEmpty(proxyUrl))
            {
                this.Data.Add("Forge:ProxyUrl", proxyUrl);
            }
            var proxyUser = Environment.GetEnvironmentVariable("FORGE_PROXY_USER");
            if (!string.IsNullOrEmpty(proxyUser))
            {
                this.Data.Add("Forge:ProxyUser", proxyUser);
            }
            var proxyPass = Environment.GetEnvironmentVariable("FORGE_PROXY_PASS");
            if (!string.IsNullOrEmpty(proxyPass))
            {
                this.Data.Add("Forge:ProxyPass", proxyPass);
            }
        }
    }
}

