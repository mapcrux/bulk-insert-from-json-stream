using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;

namespace TiCRateParser
{
    public interface IProviderParser
    {
        IEnumerable<Provider> ParseProviderGroups(JObject jsonNode);
        Dictionary<string, IEnumerable<Guid>> ParseProviderArray(ITargetBlock<Provider> providerTarget, JsonTextReader jsonReader);
    }
    public class ProviderParser : IProviderParser
    {
        private readonly ILogger<ProviderParser> logger;
        private readonly JsonSerializer jsonSerializer;
        public ProviderParser(ILogger<ProviderParser> logger)
        {
            this.logger = logger;
            jsonSerializer = new JsonSerializer();
        }

        public Dictionary<string, IEnumerable<Guid>> ParseProviderArray(ITargetBlock<Provider> providerTarget, JsonTextReader jsonReader)
        {
            Dictionary<string, IEnumerable<Guid>> providerDict = new Dictionary<string, IEnumerable<Guid>>();
            while (jsonReader.Read() && jsonReader.TokenType != JsonToken.EndArray)
            {
                if (jsonReader.TokenType != JsonToken.StartObject) continue;
                var providerGroupsNode = jsonSerializer.Deserialize<JObject>(jsonReader);
                var provider_group_id = providerGroupsNode?.GetValue("provider_group_id")?.Value<string>();
                var providerGroups = ParseProviderGroups(providerGroupsNode);
                foreach (var provider in providerGroups)
                {
                    providerTarget.Post(provider);
                }
                if(!string.IsNullOrEmpty(provider_group_id) && !providerDict.ContainsKey(provider_group_id))
                {
                    providerDict[provider_group_id] = providerGroups.Select(x => x.Id);
                }
            }
            return providerDict;
        }

        public IEnumerable<Provider> ParseProviderGroups(JObject node)
        {

            List<Provider> providers = new List<Provider>();
            if (node.ContainsKey("provider_groups") && node["provider_groups"].Type == JTokenType.Array)
            {
                var provider_groups = node["provider_groups"].AsEnumerable();
                foreach (var provider_group in provider_groups)
                {
                    if (provider_group.Type == JTokenType.Object)
                    {
                        Provider provider = ParseProviderGroup(provider_group as JObject);
                        providers.Add(provider);
                    }
                }
            }
            return providers;
        }

        private Provider ParseProviderGroup(JObject provider_group)
        {
            try
            {
                var npiNode = provider_group["npi"]?.AsEnumerable();
                var provider = new Provider
                (
                    provider_group["tin"]?["value"]?.Value<string>()?.Truncate(10),
                    provider_group["tin"]?["type"]?.Value<string>()?.Truncate(3),
                    npiNode != null ? npiNode.Select(x => x.Value<int>()) : new int[0]
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