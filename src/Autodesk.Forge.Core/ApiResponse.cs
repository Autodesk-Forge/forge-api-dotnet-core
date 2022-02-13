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
    /// API Response
    /// </summary>
    public class ApiResponse : IDisposable
    {
        public HttpResponseMessage HttpResponse { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResponse&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="response">Http response message.</param>
        public ApiResponse(HttpResponseMessage response)
        {
            this.HttpResponse = response;
        }

        public void Dispose()
        {
            HttpResponse?.Dispose();
        }
    }

    /// <summary>
    /// API Response
    /// </summary>
    public class ApiResponse<T> : ApiResponse
    {
        /// <summary>
        /// Gets content (parsed HTTP body)
        /// </summary>
        /// <value>The data.</value>
        public T Content { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResponse&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="response">Http response message.</param>
        /// <param name="data">content (parsed HTTP body)</param>
        public ApiResponse(HttpResponseMessage response, T content)
            : base(response)
        {
            this.Content = content;
        }

    }
}
