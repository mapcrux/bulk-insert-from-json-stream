using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace TiCRateParser
{
    public interface IRateReader
    {
        Task<ReportingEntity> ParseStream(JsonTextReader jsonReader, ITargetBlock<Provider> providerTarget, ITargetBlock<Rate> rateTarget);
    }

    public class RateReader : IRateReader
    {
        private ILogger logger;
        private IProviderParser providerParser;
        private IRateParser rateParser;

        public RateReader(ILogger<RateReader> logger, IProviderParser providerParser, IRateParser rateParser)
        {
            this.logger = logger;
            this.providerParser = providerParser;
            this.rateParser = rateParser;
        }

        public async Task<ReportingEntity> ParseStream(JsonTextReader jsonReader, ITargetBlock<Provider> providerTarget, ITargetBlock<Rate> rateTarget)
        {
            ReportingEntity entity = new ReportingEntity();
            Dictionary<string, IEnumerable<Guid>> providerDict = new Dictionary<string, IEnumerable<Guid>>();
            do
            {
                jsonReader.Read();
                if (jsonReader.Value == null) continue;
                else if (jsonReader.TokenType == JsonToken.PropertyName && jsonReader.Value.ToString() == "reporting_entity_name")
                {
                    jsonReader.Read();
                    entity.Name = jsonReader.Value.ToString();
                    logger.LogInformation($"Found entity name {entity.Name}");
                }
                else if (jsonReader.TokenType == JsonToken.PropertyName && jsonReader.Value.ToString() == "reporting_entity_type")
                {
                    jsonReader.Read();
                    entity.Type = jsonReader.Value.ToString();
                    logger.LogInformation($"Found entity type {entity.Type}");
                }
                else if (jsonReader.TokenType == JsonToken.PropertyName && jsonReader.Value.ToString() == "last_updated_on")
                {
                    jsonReader.Read();
                    var last_updated_on_node = jsonReader.Value.ToString();
                    if (last_updated_on_node != null && last_updated_on_node.Length == 10)
                    {
                        entity.LastUpdatedOn = DateOnly.Parse(last_updated_on_node);
                        logger.LogInformation($"Found last updated date {entity.LastUpdatedOn}");
                    }
                }
                else if (jsonReader.TokenType == JsonToken.PropertyName && jsonReader.Value.ToString() == "provider_references")
                {
                    logger.LogInformation("Found provider section in document");
                    jsonReader.Read();
                    if (jsonReader.TokenType == JsonToken.StartArray)
                    {
                        providerDict = providerParser.ParseProviderArray(providerTarget, jsonReader);
                    }
                }
                else if (jsonReader.TokenType == JsonToken.PropertyName && jsonReader.Value.ToString() == "in_network")
                {
                    logger.LogInformation("Found rates section in document");
                    jsonReader.Read();
                    if (jsonReader.TokenType == JsonToken.StartArray)
                    {
                        rateParser.ParseRates(providerTarget, rateTarget, providerDict, jsonReader, entity.Id);
                    }
                }
            } while (!jsonReader.TokenType.Equals(JsonToken.None));
            return entity;
        }

        private ReportingEntity ParsePreamble(JsonNode preambleNode)
        {
            var reporting_entity = new ReportingEntity();
            reporting_entity.Name = preambleNode?["reporting_entity_name"]?.GetValue<string>()?.Truncate(100);
            reporting_entity.Type = preambleNode?["reporting_entity_type"]?.GetValue<string>()?.Truncate(50);
            var last_updated_on_node = preambleNode?["last_updated_on"]?.GetValue<string>();
            if (last_updated_on_node != null && last_updated_on_node.Length == 10)
            {
                reporting_entity.LastUpdatedOn = DateOnly.Parse(last_updated_on_node);
            }
            return reporting_entity;
        }

        //public abstract void RateNodeProducer(ITargetBlock<JsonNode> target);
        //public abstract void ProviderNodeProducer(ITargetBlock<JsonNode> target);
    }

}
