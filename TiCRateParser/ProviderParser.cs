using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;

namespace TiCRateParser
{
    public interface IProviderParser
    {
        IEnumerable<Provider> ParseProviderGroup(JsonNode provider_groups);
        Provider ParseProvider(JsonNode provider_group);
    }
    public class ProviderParser : IProviderParser
    {
        private readonly ILogger<ProviderParser> logger;
        public ProviderParser(ILogger<ProviderParser> logger)
        {
            this.logger = logger;
        }

        public IEnumerable<Provider> ParseProviderGroup(JsonNode node)
        {

            List<Provider> providers = new List<Provider>();
            var provider_groups = node?["provider_groups"]?.AsArray();
            if (provider_groups != null && provider_groups.Count > 0)
            {
                foreach (var provider_group in provider_groups)
                {
                    Provider provider = ParseProvider(provider_group);
                    providers.Add(provider);
                }
            }
            return providers;
        }

        public Provider ParseProvider(JsonNode provider_group)
        {
            try
            {
                var tinNode = provider_group["tin"];
                var npiNode = provider_group["npi"]?.AsArray();
                var provider = new Provider
                (
                    tinNode["value"]?.GetValue<string>().Truncate(10),
                    tinNode["type"]?.GetValue<string>().Truncate(3),
                    (npiNode != null) ? npiNode.Select(x => x.GetValue<int>()) : new int[0]
                );
                return provider;
            }
            catch(Exception e)
            {
                logger.LogDebug(e, $"Failed to parse provider");
                return null;
            }
        }

    }
}