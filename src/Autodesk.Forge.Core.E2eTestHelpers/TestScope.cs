using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Autodesk.Forge.Core.E2eTestHelpers
{
    public interface ITestScope
    {
        Task<HttpResponseMessage> SendAsync(HttpMessageInvoker inner, HttpRequestMessage request, CancellationToken cancellationToken);
    }

    internal abstract class TestScope : ITestScope, IDisposable
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
        
    }
}
