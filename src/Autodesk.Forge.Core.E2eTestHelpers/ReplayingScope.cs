using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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
            if (response!=null)
            {
                return Task.FromResult(response);
            }

            if (this.responseIndex == 0)
            {
                var json = File.ReadAllText(base.path);
                this.records = JsonConvert.DeserializeObject<List<HttpResponseMessage>>(json, new HttpResponeMessageConverter());
            }
            return Task.FromResult(this.records[this.responseIndex++]);
        }
    }
}