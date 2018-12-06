using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Autodesk.Forge.Core
{
    public abstract class Configuration
    {
        public Configuration(HttpClient client = null)
        {
            this.Client = client ?? ConfigureDefaultPipeline();
            this.ForgeAuthenticationAddress = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate");
        }
        public string ForgeKey { get; set; }
        public string ForgeSecret { get; set; }
        public Uri ForgeAuthenticationAddress { get; set; }

        public HttpClient Client { get; private set; }

        private HttpClient ConfigureDefaultPipeline()
        {
            return new HttpClient(new ForgeHandler(this)
            {
                InnerHandler = new HttpClientHandler()
            });
        }
    }
}
