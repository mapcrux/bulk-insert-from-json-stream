using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;

namespace TiCRateParser
{


    public interface IRateParser
    {
        void ParseRates(ITargetBlock<Provider> providerTarget, ITargetBlock<Rate> rateTarget, Dictionary<string,IEnumerable<Guid>> providerDict, JsonTextReader jsonReader, Guid entityId);
    }
    public class RateParser : IRateParser
    {

        private readonly JsonSerializer jsonSerializer;
        private readonly IProviderParser providerParser;
        private readonly ILogger logger;
        public RateParser(ILogger<RateParser> logger, IProviderParser providerParser)
        {
            this.logger = logger;
            this.providerParser = providerParser;
            jsonSerializer = new JsonSerializer();
        }

        public void ParseRates(ITargetBlock<Provider> providerTarget, ITargetBlock<Rate> rateTarget, Dictionary<string, IEnumerable<Guid>> providerDict, JsonTextReader jsonReader, Guid entityId)
        {
            while (jsonReader.Read() && jsonReader.TokenType != JsonToken.EndArray)
            {
                if (jsonReader.TokenType != JsonToken.StartObject) continue;
                var node = jsonSerializer.Deserialize<JObject>(jsonReader);
                var rates = ParseRatesForCode(providerTarget, providerDict, entityId, node);
                foreach(var rate in rates)
                {
                    rateTarget.Post(rate);
                }
            }
            providerTarget.Complete();
            rateTarget.Complete();
        }

        private IEnumerable<Rate> ParseRatesForCode(ITargetBlock<Provider> providerTarget, Dictionary<string, IEnumerable<Guid>> providerDict, Guid entityId, JObject? node)
        {
            List<Rate> rates = new List<Rate>();
            try
            {
                var negotiated_arrangement = node?["negotiation_arrangement"]?.Value<string>()?.Truncate(3);
                var billing_code = node?["billing_code"]?.Value<string>()?.Truncate(7);
                var billing_code_type_version = node?["billing_code_type_version"]?.Value<string>()?.Truncate(10);
                var billing_code_type = node?["billing_code_type"]?.Value<string>()?.Truncate(7);
                var negotiated_rates_node = node?["negotiated_rates"];
                if (negotiated_rates_node != null && negotiated_rates_node.Type == JTokenType.Array)
                {
                    foreach (var rate_node in negotiated_rates_node.AsEnumerable())
                    {
                        var provider_references = rate_node?["provider_references"]?.AsEnumerable();
                        var provider_groups = rate_node?["provider_groups"];
                        IEnumerable<Guid> pids = new List<Guid>();
                        if (provider_references != null)
                        {
                            if (providerDict == null) continue;
                            pids = provider_references.Select(x =>
                                providerDict.ContainsKey(x.Value<string>()) ?
                                providerDict[x.Value<string>()] : new Guid[0])
                                .SelectMany(x => x);
                        }
                        else if (provider_groups != null && provider_groups.Type == JTokenType.Array)
                        {
                            var providerGroups = providerParser.ParseProviderGroups(provider_groups.AsEnumerable());
                            foreach (var provider in providerGroups)
                            {
                                providerTarget.Post(provider);
                            }
                            pids = providerGroups.Select(x => x.Id);
                        }
                        var negotiated_prices = rate_node?["negotiated_prices"]?.AsEnumerable();
                        if (negotiated_prices != null)
                        {
                            var prices = negotiated_prices.Select(x =>
                            new
                            {
                                negotiated_type = x["negotiated_type"]?.Value<string>().Truncate(15),
                                billing_code_modifier = string.Join(',', x["billing_code_modifier"]?.AsEnumerable()).Truncate(50),
                                additional_information = x["additional_information"]?.Value<string>().Truncate(50),
                                negotiated_rate = x["negotiated_rate"]?.Value<double>(),
                                expiration_date = x["expiration_date"]?.Value<string>().ConvertDate(),
                                billing_class = x["billing_class"]?.Value<string>().Truncate(15)
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
                                Provider = t,
                                ReportingEntity = entityId
                            }));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogDebug(e, $"Failed to parse provider");
            }
            return rates;
        }
    }
}