using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Autodesk.Forge.Core
{
    public class ForgeConfiguration
    {
        public const string ScopeKey = "Autodesk.Forge.Scope";
        public ForgeConfiguration()
        {
            this.AuthenticationAddress = new Uri("https://developer.api.autodesk.com/authentication/v1/authenticate");
        }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public Uri AuthenticationAddress { get; set; }
    }
}
