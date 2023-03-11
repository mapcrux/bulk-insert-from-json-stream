using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TiCRateParser
{
    public interface IProviderParser
    {
        void ParseProviderArray(ITargetBlock<Provider> providerTarget, JsonTextReader jsonReader);
    }
    public class ProviderParser : IProviderParser
    {
        public void ParseProviderArray(ITargetBlock<Provider> providerTarget, JsonTextReader jsonReader)
        {
            throw new NotImplementedException();
        }

        //private ProviderGroup ParseProviderGroups(JsonNode node)
        //{

        //    List<Provider> providers = new List<Provider>();
        //    var provider_groups = node?["provider_groups"]?.AsArray();
        //    if (provider_groups != null && provider_groups.Count > 0)
        //    {
        //        foreach (var provider_group in provider_groups)
        //        {
        //            Provider provider = ParseProviderGroup(provider_group);
        //            providers.Add(provider);
        //        }
        //    }
        //    return new ProviderGroup(providers);
        //}

        private Provider ParseProviderGroup(JsonNode provider_group)
        {
            var tinNode = provider_group["tin"];
            var npiNode = provider_group["npi"]?.AsArray();
            var provider = new Provider
            (
                tinNode["value"]?.GetValue<string>().Truncate(10),
                tinNode["type"]?.GetValue<string>().Truncate(3),
                npiNode != null ? npiNode.Select(x => x.GetValue<int>()) : new int[0]
            );
            return provider;
        }

    }
}