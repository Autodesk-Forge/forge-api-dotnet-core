using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Autodesk.Forge.Core.E2eTestHelpers
{
    internal class RecordingScope : TestScope
    {
        private List<JObject> records = new List<JObject>();
        private JsonSerializer serializer;

        public RecordingScope(string path)
            : base(path)
        {
            this.serializer = new JsonSerializer();
            this.serializer.Converters.Add(new HttpResponeMessageConverter());
        }

        public async override Task<HttpResponseMessage> SendAsync(HttpMessageInvoker inner, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await inner.SendAsync(request, cancellationToken);

            if (!TryRecordAuthentication(response))
            {
                var json = JObject.FromObject(response, this.serializer);
                this.records.Add(json);
            }
            return response;
        }

        public override void Dispose()
        {
            base.Dispose();
            var json = JsonConvert.SerializeObject(this.records, Formatting.Indented);
            File.WriteAllText(base.path, json);
        }

        public override bool IsRecording => true;
    }
}