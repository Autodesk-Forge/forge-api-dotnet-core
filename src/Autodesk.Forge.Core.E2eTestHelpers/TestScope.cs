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
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Autodesk.Forge.Core.E2eTestHelpers
{
    public interface IMessageProcessor
    {
        Task<HttpResponseMessage> SendAsync(HttpMessageInvoker inner, HttpRequestMessage request, CancellationToken cancellationToken);
    }

    public interface ITestScope : IDisposable
    {
        bool IsRecording { get; }
    }

    internal abstract class TestScope : IMessageProcessor, ITestScope
    {
        private HttpResponseMessage authResponse;
        protected string path;
        public TestScope(string path)
        {
            this.path = path;
            var authPath = Path.Combine(Path.GetDirectoryName(this.path), "authenticate.json");
            if (File.Exists(authPath))
            {
                var json = File.ReadAllText(authPath);
                this.authResponse = JsonConvert.DeserializeObject<HttpResponseMessage>(json, new HttpResponeMessageConverter());
            }
        }

        private bool IsAuthentication(HttpRequestMessage request)
        {
            return request.RequestUri.ToString().Contains("authenticate");
        }
        protected bool TryRecordAuthentication(HttpResponseMessage response)
        {
            if (IsAuthentication(response.RequestMessage))
            {
                if (this.authResponse == null)
                {
                    this.authResponse = response;
                    var authPath = Path.Combine(Path.GetDirectoryName(this.path), "authenticate.json");
                    File.WriteAllText(authPath, JsonConvert.SerializeObject(this.authResponse, Formatting.Indented, new HttpResponeMessageConverter()));
                }
                return true;
            }
            return false;
        }
        protected HttpResponseMessage TryGetAuthentication(HttpRequestMessage request)
        {
            if (IsAuthentication(request))
            {
                return this.authResponse;
            }
            return null;
        }

        public abstract Task<HttpResponseMessage> SendAsync(HttpMessageInvoker inner, HttpRequestMessage request, CancellationToken cancellationToken);
        
        public virtual void Dispose()
        {
            authResponse?.Dispose();
        }

        public abstract bool IsRecording { get; }

    }
}
