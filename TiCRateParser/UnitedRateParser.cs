using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;

namespace TiCRateParser
{
    public class UnitedRateParser : IRateParser
    {
        public Dictionary<int, Provider> providers = new Dictionary<int, Provider>();
        public string reporting_entity_name;
        public string reporting_entity_type;
        public DateOnly last_updated_on;
        private string fileLocation;
        private string processingPath = "./processing.json";

        public UnitedRateParser(string fileLocation)
        {
            this.fileLocation = fileLocation;
        }

        public async Task FilePrep()
        {

            string preamble = "";
            await using FileStream fileread = File.OpenRead(fileLocation);
            await using FileStream filewrite = File.Create(processingPath);
            using (StreamReader reader = new StreamReader(fileread))
            {
                using (StreamWriter writer = new StreamWriter(filewrite))
                {
                    string? line = reader.ReadLine();
                    while (!line.TrimStart().StartsWith("\"in_network\""))
                    {
                        preamble += line;
                        line = reader.ReadLine();
                    }
                    preamble += "}";

                    writer.WriteLine("[");
                    do
                    {
                        line = reader.ReadLine();
                        if (reader.Peek() != -1)
                        {
                            writer.WriteLine(line);
                        }

                    } while (line != null);
                }
            }
            ParsePreamble(preamble);
        }

        public void ParsePreamble(string preambleString)
        {
            var preambleNode = JsonNode.Parse(preambleString);
            reporting_entity_name = preambleNode?["reporting_entity_name"]?.GetValue<string>()?.Truncate(100);
            reporting_entity_type = preambleNode?["reporting_entity_type"]?.GetValue<string>()?.Truncate(50);
            var last_updated_on_node = preambleNode?["last_updated_on"]?.GetValue<string>();
            if(last_updated_on_node != null && last_updated_on_node.Length == 10)
            {
                last_updated_on = DateOnly.Parse(last_updated_on_node);
            }
            var provider_node = preambleNode?["provider_references"];
            if (provider_node != null)
            {
                providers = ParseProviderSection.ParseProviderArray(provider_node.AsArray());
            }
        }

        public async Task Produce(ITargetBlock<Rate> target)
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
            IEnumerable<Rate> rates = new List<Rate>();
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
                    var provider_groups = rate_node?["provider_references"]?.AsArray();
                    if (provider_groups != null)
                    {
                        var pids = provider_groups.Select(x => x.GetValue<int>());
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
                            rates = pids.SelectMany(t => prices, (t, p) => new Rate
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
                                ProviderID = providers[t].Id
                            });
                        }
                    }
                }
            }
            return rates;
        }
    }
}
