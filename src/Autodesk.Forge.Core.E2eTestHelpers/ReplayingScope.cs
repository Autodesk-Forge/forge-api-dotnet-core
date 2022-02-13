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
using Newtonsoft.Json.Linq;
using Xunit;

namespace Autodesk.Forge.Core.E2eTestHelpers
{
    internal class ReplayingScope : TestScope
    {
        private int responseIndex;
        private List<HttpResponseMessage> records;

        public ReplayingScope(string path)
            : base(path)
        {
        }

        public override Task<HttpResponseMessage> SendAsync(HttpMessageInvoker inner, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = TryGetAuthentication(request);
            if (response == null)
            {
                if (this.responseIndex == 0)
                {
                    var json = File.ReadAllText(base.path);
                    this.records = JsonConvert.DeserializeObject<List<HttpResponseMessage>>(json, new HttpResponeMessageConverter());
                }
                response = this.records[this.responseIndex++];
                AssertEqual(response.RequestMessage, request);
            }
            return Task.FromResult(response);
        }

        private void AssertEqual(HttpRequestMessage recorded, HttpRequestMessage incoming)
        {
            Assert.Equal(recorded.Method, incoming.Method);
            Assert.Equal(recorded.RequestUri, incoming.RequestUri);
            var jRecorded = HttpResponeMessageConverter.SerializeRequest(recorded);
            var jIncoming = HttpResponeMessageConverter.SerializeRequest(incoming);
            Assert.True(JToken.DeepEquals(jRecorded, jIncoming));
        }

        public override bool IsRecording => false;
    }
}