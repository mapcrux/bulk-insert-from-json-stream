using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json;

namespace TiCRateParser
{
    public abstract class RateReader
    {
        
        protected async Task ParseStream(JsonTextReader jsonReader, ReportingEntity entity, ITargetBlock<Provider> providerTarget, ITargetBlock<Rate> rateTarget)
        {
            do
            {
                jsonReader.Read();
                if (jsonReader.Value == null) continue;
                else if (jsonReader.TokenType == JsonToken.PropertyName && jsonReader.Value.ToString() == "reporting_entity_name")
                {
                    jsonReader.Read();
                    entity.Name = jsonReader.Value.ToString();
                }
                else if (jsonReader.TokenType == JsonToken.PropertyName && jsonReader.Value.ToString() == "reporting_entity_type")
                {
                    jsonReader.Read();
                    entity.Type = jsonReader.Value.ToString();
                }
                else if (jsonReader.TokenType == JsonToken.PropertyName && jsonReader.Value.ToString() == "last_updated_on")
                {
                    jsonReader.Read();
                    var last_updated_on_node = jsonReader.Value.ToString();
                    if (last_updated_on_node != null && last_updated_on_node.Length == 10)
                    {
                        entity.LastUpdatedOn = DateOnly.Parse(last_updated_on_node);
                    }
                }
                else if (jsonReader.TokenType == JsonToken.PropertyName && jsonReader.Value.ToString() == "provider_references")
                {
                    jsonReader.Read();
                    if (jsonReader.TokenType == JsonToken.StartArray)
                    {
                        //providerDict = providerParser.ParseProviderArray(providerTarget, jsonReader);
                    }
                }
                else if (jsonReader.TokenType == JsonToken.PropertyName && jsonReader.Value.ToString() == "in_network")
                {
                    jsonReader.Read();
                    if (jsonReader.TokenType == JsonToken.StartArray)
                    {
                        //rateParser.ParseRates(jsonReader, providerTarget, rateTarget, providerDict);
                    }
                }
            } while (!jsonReader.TokenType.Equals(JsonToken.None));
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
