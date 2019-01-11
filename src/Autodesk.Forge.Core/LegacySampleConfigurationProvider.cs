using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Autodesk.Forge.Core
{
    public static class ForgeAlternativeConfigurationExtensions
    {
        public static IConfigurationBuilder AddForgeAlternativeEnvironmentVariables(this IConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Add(new ForgeAlternativeConfigurationSource());
            return configurationBuilder;
        }
    }

    public class ForgeAlternativeConfigurationSource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new ForgeAlternativeConfigurationProvider();
        }
    }

    public class ForgeAlternativeConfigurationProvider : ConfigurationProvider
    {
        public override void Load()
        {
            var id = Environment.GetEnvironmentVariable("FORGE_CLIENT_ID");
            if (!string.IsNullOrEmpty(id))
            {
                this.Data.Add("Forge:ClientId", id);
            }
            var secret = Environment.GetEnvironmentVariable("FORGE_CLIENT_SECRET");
            if (!string.IsNullOrEmpty(secret))
            {
                this.Data.Add("Forge:ClientSecret", secret);
            }
        }
    }
}

