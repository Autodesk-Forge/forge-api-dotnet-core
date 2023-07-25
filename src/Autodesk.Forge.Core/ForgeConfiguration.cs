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

namespace Autodesk.Forge.Core
{
    public class ForgeConfiguration
    {
        public static readonly HttpRequestOptionsKey<string> AgentKey = new HttpRequestOptionsKey<string>("Autodesk.Forge.Agent");
        public static readonly HttpRequestOptionsKey<string> ScopeKey = new HttpRequestOptionsKey<string>("Autodesk.Forge.Scope");
        public static readonly HttpRequestOptionsKey<int> TimeoutKey = new HttpRequestOptionsKey<int>("Autodesk.Forge.Timeout");

        public ForgeConfiguration()
        {
            this.AuthenticationAddress = new Uri("https://developer.api.autodesk.com/authentication/v2/token");
        }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public IDictionary<string, ForgeAgentConfiguration> Agents { get; set; }
        public Uri AuthenticationAddress { get; set; }
    }
}
