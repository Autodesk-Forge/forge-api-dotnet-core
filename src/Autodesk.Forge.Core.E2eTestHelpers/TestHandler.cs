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
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Autodesk.Forge.Core.E2eTestHelpers
{
    public class TestHandler : DelegatingHandler
    {
        private string basePath;
        private readonly AsyncLocal<IMessageProcessor> testScope = new AsyncLocal<IMessageProcessor>();
        public ITestScope StartTestScope(string name)
        {
            TestScope scope;
            var path = Path.Combine(this.basePath, $"{name}.json");
            if (File.Exists(path))
            {
                scope = new ReplayingScope(path);
            }
            else
            {
                scope = new RecordingScope(path);
            }
            this.testScope.Value = scope;
            return scope;
        }
        public TestHandler(string basePath)
            : base(new HttpClientHandler())
        {
            this.basePath = basePath;
            if (!Directory.Exists(basePath))
            {
                throw new ArgumentException($"Folder with recordings does not exist. Looked for it here: {basePath}");
            }
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (this.testScope.Value == null)
            {
                throw new InvalidOperationException("TestScope is null. Did you forget to call StartTestScope?");
            }
            return this.testScope.Value.SendAsync(new HttpMessageInvoker(InnerHandler), request, cancellationToken);
        }
    }
}
