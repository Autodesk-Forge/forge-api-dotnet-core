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
    /// <summary>
    /// Represents a handler for Forge agents.
    /// </summary>
    public class ForgeAgentHandler : DelegatingHandler
    {
        /// <summary>
        /// The default agent name.
        /// </summary>
        public const string defaultAgentName = "default";

        private string user;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForgeAgentHandler"/> class.
        /// </summary>
        /// <param name="user">The user associated with the agent.</param>
        public ForgeAgentHandler(string user)
        {
            this.user = user;
        }

        /// <summary>
        /// Sends an HTTP request asynchronously.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task representing the asynchronous operation.</returns>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Options.TryAdd(ForgeConfiguration.AgentKey.Key, user);
            return base.SendAsync(request, cancellationToken);
        }
    }
}

