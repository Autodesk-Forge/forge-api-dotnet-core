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

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Autodesk.Forge.Core
{
    public class ForgeAgentHandler : DelegatingHandler
    {
        public const string defaultAgentName = "default";
        
        private string user;
        public ForgeAgentHandler(string user)
        {
            this.user = user;
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Properties.Add(ForgeConfiguration.AgentKey, user);
            return base.SendAsync(request, cancellationToken);
        }
    }
}

