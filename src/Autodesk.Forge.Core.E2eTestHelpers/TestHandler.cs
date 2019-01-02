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
        private readonly AsyncLocal<ITestScope> testScope = new AsyncLocal<ITestScope>();
        public IDisposable StartScope(string name)
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
            :base(new HttpClientHandler())
        {
            this.basePath = basePath;
            if (!Directory.Exists(basePath))
            {
                throw new ArgumentException($"Folder with recordings does not exist. Looked for it here: {basePath}");
            }
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return this.testScope.Value.SendAsync(new HttpMessageInvoker(InnerHandler), request, cancellationToken);
        }
        }
    }
