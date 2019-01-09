using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Autodesk.Forge.Core
{
    public static class LegacySampleConfigurationExtensions
    {
        public static IConfigurationBuilder AddLegacySampleEnvironmentVariables(this IConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Add(new LegacySampleConfigurationSource());
            return configurationBuilder;
        }
    }

    public class LegacySampleConfigurationSource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new LegacySampleConfigurationProvider();
        }
    }

    public class LegacySampleConfigurationProvider : ConfigurationProvider
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

