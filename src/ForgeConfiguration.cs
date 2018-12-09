using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Autodesk.Forge.Core
{
    public class ForgeConfiguration
    {
        public const string ForgeScopeHttpRequestPropertyKey = "Autodesk.Forge.Scope";
        public ForgeConfiguration()
        {
            this.ForgeAuthenticationAddress = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate");
        }
        public string ForgeKey { get; set; }
        public string ForgeSecret { get; set; }
        public Uri ForgeAuthenticationAddress { get; set; }
        public Uri BaseAddress { get; set; }
    }
}
