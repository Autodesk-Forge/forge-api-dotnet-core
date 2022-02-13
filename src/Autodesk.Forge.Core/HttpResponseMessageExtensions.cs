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
using System.Net;

namespace Autodesk.Forge.Core
{
    public static class HttpResponseMessageExtensions
    {
        public static async Task<HttpResponseMessage> EnsureSuccessStatusCodeAsync(this HttpResponseMessage msg)
        {
            string errorMessage = string.Empty;
            if (!msg.IsSuccessStatusCode)
            {
                // Disposing content just like HttpResponseMessage.EnsureSuccessStatusCode
                if (msg.Content != null)
                {
                    // read more detailed error message if available 
                    errorMessage = await msg.Content.ReadAsStringAsync();
                    msg.Content.Dispose();
                }
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = $"\nMore error details:\n{errorMessage}.";
                }
                var message = $"The server returned the non-success status code {(int)msg.StatusCode} ({msg.ReasonPhrase}).{errorMessage}";

                if (msg.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var retryAfterHeader = msg.Headers.RetryAfter.Delta;
                    throw new TooManyRequestsException(message, msg.StatusCode, retryAfterHeader);
                }
                
                throw new HttpRequestException(message, null, msg.StatusCode);
            }
            return msg;
        }
    }

    public class TooManyRequestsException : HttpRequestException
    {
        public TooManyRequestsException(string message, HttpStatusCode statusCode, TimeSpan? retryAfter)
            :base(message, null, statusCode)
        {
            this.RetryAfter = retryAfter;
        }
        
        public TimeSpan? RetryAfter { get; init; }
    }
}
