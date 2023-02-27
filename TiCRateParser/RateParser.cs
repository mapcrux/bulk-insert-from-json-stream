using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TiCRateParser
{


    public interface IRateParser
    {
        public string reporting_entity_name { get; set; }
        public string reporting_entity_type { get; set; }
        public List<Provider> providers { get; }
        public DateOnly last_updated_on { get; set; }
        IEnumerable<Rate> ParseRates();
        Task ParseRatesAsync(ITargetBlock<Rate> target, string processingPath);
    }
    public class RateParser : IRateParser
    {

        protected Dictionary<int, ProviderGroup> providerDict = null;
        protected JsonArray rates_node;
        public List<Provider> providers { get; }
        public string reporting_entity_name { get; set; }
        public string reporting_entity_type { get; set; }
        public DateOnly last_updated_on { get; set; }

        public RateParser(JsonNode node)
        {
            providers = new List<Provider>();
            ParsePreamble(node);
        }

        public IEnumerable<Rate> ParseRates()
        {
            List<Rate> rates = new List<Rate>();
            if(rates_node != null)
            {
                foreach(var node in rates_node)
                {
                    rates.AddRange(ParseRatesForCode(node));
                }
                return rates;
            }
            Console.WriteLine("Cannot parse rates, no in_network node found");
            return null;
        }

        public async Task ParseRatesAsync(ITargetBlock<Rate> target, string processingPath)
        {
            await using FileStream file = File.OpenRead(processingPath);
            IAsyncEnumerable<JsonNode> enumerable = JsonSerializer.DeserializeAsyncEnumerable<JsonNode>(file);
            try
            {
                await foreach (JsonNode node in enumerable)
                {
                    var rates = ParseRatesForCode(node);
                    foreach (Rate r in rates)
                        target.Post(r);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Encountered Parse Error");
            }
            Console.WriteLine("Finished Parsing Rates");
            target.Complete();
        }

        public IEnumerable<Rate> ParseRatesForCode(JsonNode node)
        {
            List<Rate> rates = new List<Rate>();
            var negotiated_arrangement = node?["negotiation_arrangement"]?.GetValue<string>()?.Truncate(3);
            var billing_code = node?["billing_code"]?.GetValue<string>()?.Truncate(7);
            var billing_code_type_version_string = node?["billing_code_type_version"]?.GetValue<string>();
            var billing_code_type_version = (string.IsNullOrEmpty(billing_code_type_version_string)) ? 0 : Convert.ToInt32(billing_code_type_version_string);
            var billing_code_type = node?["billing_code_type"]?.GetValue<string>().Truncate(7);
            var negotiated_rates_node = node?["negotiated_rates"];
            if (!string.IsNullOrEmpty(billing_code) && negotiated_rates_node != null)
            {
                foreach (var rate_node in negotiated_rates_node.AsArray())
                {
                    var provider_references = rate_node?["provider_references"]?.AsArray();
                    var provider_groups = rate_node?["provider_groups"]?.AsArray();
                    IEnumerable<Provider> pids = new List<Provider>();
                    if (provider_references != null)
                    {
                        if (providerDict == null)
                            return rates;
                        pids = provider_references.Select(x => providerDict[x.GetValue<int>()]).SelectMany(x => x.providers);
                    }
                    else if (provider_groups != null)
                    {
                        pids = provider_groups.Select(x => ParseProviderSection.ParseProviderGroup(x));
                        providers.AddRange(pids);
                    }
                    var negotiated_prices = rate_node?["negotiated_prices"]?.AsArray();
                    if (negotiated_prices != null)
                    {
                        var prices = negotiated_prices.Select(x =>
                        new
                        {
                            negotiated_type = x["negotiated_type"]?.GetValue<string>().Truncate(15),
                            service_code = x["service_code"]?.AsArray().ToJsonString().Truncate(15),
                            billing_code_modifier = x["billing_code_modifier"]?.AsArray().ToJsonString().Truncate(50),
                            additional_information = x["additional_information"]?.GetValue<string>().Truncate(50),
                            negotiated_rate = x["negotiated_rate"]?.GetValue<double>(),
                            expiration_date = x["expiration_date"]?.GetValue<string>().ConvertDate(),
                            billing_class = x["billing_class"]?.GetValue<string>().Truncate(15)
                        });
                        rates.AddRange(pids.SelectMany(t => prices, (t, p) => new Rate
                        {
                            BillingClass = p.billing_class,
                            NegotiatedArrangement = negotiated_arrangement,
                            BillingCode = billing_code,
                            BillingCodeType = billing_code_type,
                            BillingCodeTypeVersion = billing_code_type_version,
                            ExpirationDate = p.expiration_date,
                            NegotiatedRate = p.negotiated_rate,
                            NegotiatedType = p.negotiated_type,
                            BillingCodeModifier = p.billing_code_modifier,
                            AdditionalInformation = p.additional_information,
                            Provider = t.Id
                        }));
                    }

                }
            }
            return rates;
        }

        private void ParsePreamble(JsonNode preambleNode)
        {
            reporting_entity_name = preambleNode?["reporting_entity_name"]?.GetValue<string>()?.Truncate(100);
            reporting_entity_type = preambleNode?["reporting_entity_type"]?.GetValue<string>()?.Truncate(50);
            var last_updated_on_node = preambleNode?["last_updated_on"]?.GetValue<string>();
            rates_node = preambleNode?["in_network"]?.AsArray();
            if (last_updated_on_node != null && last_updated_on_node.Length == 10)
            {
                last_updated_on = DateOnly.Parse(last_updated_on_node);
            }
            var provider_node = preambleNode?["provider_references"];
            if (provider_node != null)
            {
                ParseProviderArray(provider_node.AsArray());
            }
        }



        private void ParseProviderArray(JsonArray array)
        {
            Console.WriteLine("Beginning Parsing Providers");
            providerDict = new Dictionary<int, ProviderGroup>();
            foreach (JsonNode node in array)
            {
                var id = node["provider_group_id"]?.GetValue<int>();
                if (id != null && !providerDict.ContainsKey(id.Value))
                {
                    var group = ParseProviderSection.ParseProviderGroups(node);
                    providerDict[id.Value] = group;
                    providers.AddRange(group.providers);
                }
            }
            Console.WriteLine("Finished Parsing Providers");
        }
    }
}