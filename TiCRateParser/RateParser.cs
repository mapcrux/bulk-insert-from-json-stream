using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;

namespace TiCRateParser
{


    public interface IRateParser
    {
        RateSection ParseRatesForCode(JsonNode node, Dictionary<string, IEnumerable<Guid>> providerDict);
    }
    public class RateParser : IRateParser
    {

        private readonly IProviderParser providerParser;
        private readonly ILogger logger;
        public RateParser(ILogger<RateParser> logger, IProviderParser providerParser)
        {
            this.logger = logger;
            this.providerParser = providerParser;
        }
        public RateSection ParseRatesForCode(JsonNode node, Dictionary<string, IEnumerable<Guid>> providerDict)
        {
            RateSection rs = new RateSection();
            try
            {
                var negotiated_arrangement = node?["negotiation_arrangement"]?.GetValue<string>()?.Truncate(3);
                var billing_code = node?["billing_code"]?.GetValue<string>()?.Truncate(7);
                var billing_code_type_version_string = node?["billing_code_type_version"]?.GetValue<string>();
                var billing_code_type_version = node?["billing_code_type_version"]?.GetValue<string>()?.Truncate(10);
                var billing_code_type = node?["billing_code_type"]?.GetValue<string>().Truncate(7);
                var negotiated_rates_node = node?["negotiated_rates"];
                if (!string.IsNullOrEmpty(billing_code) && negotiated_rates_node != null)
                {
                    foreach (var rate_node in negotiated_rates_node.AsArray())
                    {
                        var provider_references = rate_node?["provider_references"]?.AsArray();
                        var provider_groups = rate_node?["provider_groups"]?.AsArray();
                        IEnumerable<Guid> pids = new List<Guid>();
                        if (provider_references != null)
                        {
                            if (providerDict == null)
                                return rs;
                            pids = provider_references.Select(x => providerDict[x.GetValue<string>()]).SelectMany(x => x);
                        }
                        else if (provider_groups != null)
                        {
                            var providers = provider_groups.Select(x => providerParser.ParseProvider(x));
                            pids = providers.Select(x => x.Id);
                            rs.providers.AddRange(providers);
                        }
                        var negotiated_prices = rate_node?["negotiated_prices"]?.AsArray();
                        if (negotiated_prices != null)
                        {
                            var prices = negotiated_prices.Select(x =>
                            new
                            {
                                negotiated_type = x["negotiated_type"]?.GetValue<string>().Truncate(15),
                                service_code = x["service_code"]?.AsArray()?.ToJsonString().Truncate(15),
                                billing_code_modifier = x["billing_code_modifier"]?.AsArray().ToJsonString().Truncate(50),
                                additional_information = x["additional_information"]?.GetValue<string>().Truncate(50),
                                negotiated_rate = x["negotiated_rate"]?.GetValue<double>(),
                                expiration_date = x["expiration_date"]?.GetValue<string>().ConvertDate(),
                                billing_class = x["billing_class"]?.GetValue<string>().Truncate(15)
                            });
                            rs.rates.AddRange(pids.SelectMany(t => prices, (t, p) => new Rate
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
                                Provider = t
                            }));
                        }

                    }
                }
            }
            catch(Exception e)
            {
                logger.LogDebug(e, "Failed parsing rate for code");
            }
            return rs;
        }
    }
}