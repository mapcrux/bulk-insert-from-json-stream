using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace TiCRateParser
{
    public class AnthemNetworkParser : INetworkParser
    {
        public IEnumerable<Rate> ParseRates(JsonNode node)
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
                                ProviderID = t
                            });
                        }
                    }
                }
            }
            Provider p = new Provider();
            p.
            return rates;
        }
    }
}
