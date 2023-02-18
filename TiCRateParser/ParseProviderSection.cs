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


        public static IEnumerable<Provider> ParseProviderArray(string providerString)
        {

            JsonArray array = JsonNode.Parse(providerString).AsArray();
            Console.WriteLine("Beginning Parsing Providers");
            List<Provider> providers = new List<Provider>();
            foreach (JsonNode node in array)
            {
                var id = node["provider_group_id"]?.GetValue<int>();
                var provider_groups = node?["provider_groups"]?.AsArray();
                if (id != null && provider_groups != null && provider_groups.Count > 0)
                {
                    foreach (var provider_group in provider_groups)
                    {
                        var tinNode = provider_group["tin"];
                        var npiNode = provider_group["npi"]?.AsArray();
                        var provider = new Provider
                        {
                            ProviderID = id.Value,
                            TIN = tinNode["value"]?.GetValue<string>().Truncate(10),
                            TinType = tinNode["type"]?.GetValue<string>().Truncate(3),
                            NPIs = (npiNode != null) ? npiNode.Select(x => x.GetValue<int>()) : new int[0]
                        };
                        providers.Add(provider);
                    }
                }
            }
            Console.WriteLine("Finished Parsing Providers");
            return providers;
        }
    }
}