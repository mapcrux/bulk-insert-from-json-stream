using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace TiCRateParser
{
    public static class ParseProviderSection
    {


        public static Dictionary<int, Provider> ParseProviderArray(JsonArray array)
        {
            Console.WriteLine("Beginning Parsing Providers");
            List<Provider> providers = new List<Provider>();
            foreach (JsonNode node in array)
            {
                var id = node["provider_group_id"]?.GetValue<int>();
                providers.AddRange(ParseProviderGroups(node, id));
            }
            Console.WriteLine("Finished Parsing Providers");
            return providers.ToDictionary(x => x.ProviderID, x => x);
        }

        public static IEnumerable<Provider> ParseProviderGroups(JsonNode node, int? id)
        {

            List<Provider> providers = new List<Provider>();
            var provider_groups = node?["provider_groups"]?.AsArray();
            if (id != null && provider_groups != null && provider_groups.Count > 0)
            {
                foreach (var provider_group in provider_groups)
                {
                    Provider provider = ParseProviderGroup(id, provider_group);
                    providers.Add(provider);
                }
            }
            return providers;
        }

        public static Provider ParseProviderGroup(int? id, JsonNode provider_group)
        {
            var tinNode = provider_group["tin"];
            var npiNode = provider_group["npi"]?.AsArray();
            var provider = new Provider
            {
                ProviderID = (id.HasValue) ? id.Value : 0,
                TIN = tinNode["value"]?.GetValue<string>().Truncate(10),
                TinType = tinNode["type"]?.GetValue<string>().Truncate(3),
                NPIs = (npiNode != null) ? npiNode.Select(x => x.GetValue<int>()) : new int[0]
            };
            return provider;
        }
    }
}