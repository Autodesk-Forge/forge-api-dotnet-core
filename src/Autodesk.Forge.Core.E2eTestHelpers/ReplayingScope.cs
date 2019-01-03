using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
            Assert.Equal(jRecorded["Headers"]?.ToString(), jIncoming["Headers"]?.ToString());
            Assert.Equal(jRecorded["Content"] == null, jIncoming["Content"] == null);
            if (jRecorded["Content"] != null)
            {
                Assert.Equal(jRecorded["Content"]["Headers"] == null, jIncoming["Content"]["Headers"] == null);
                if (jRecorded["Content"]["Headers"] != null)
                {
                    Assert.Equal(jRecorded["Content"]["Headers"].ToString(), jIncoming["Content"]["Headers"].ToString());
                }
                Assert.Equal(jRecorded["Content"]["Body"] == null, jIncoming["Content"]["Body"] == null);
                if (jRecorded["Content"]["Body"] != null)
                {
                    Assert.Equal(jRecorded["Content"]["Body"].ToString(), jIncoming["Content"]["Body"].ToString());
                }
            }
        }

        public override bool IsRecording => false;
    }
}