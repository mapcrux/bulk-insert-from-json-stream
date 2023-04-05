using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;

namespace TiCRateParser
{
    public interface IRateReader
    {
        ReportingEntity ParsePreamble(FileInfo fileInfo);
        Task ParseStream(ITargetBlock<Provider> providerTarget, ITargetBlock<Rate> rateTarget, FileInfo fileInfo);
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

        public ReportingEntity ParsePreamble(FileInfo fileInfo)
        {
            var reportingEntity = new ReportingEntity();
            var preambleNode = JsonNode.Parse(fileInfo.Preamble,new JsonNodeOptions { },new JsonDocumentOptions { AllowTrailingCommas = true });
            reportingEntity.Name = preambleNode?["reporting_entity_name"]?.GetValue<string>()?.Truncate(100);
            reportingEntity.Type = preambleNode?["reporting_entity_type"]?.GetValue<string>()?.Truncate(50);
            var last_updated_on_node = preambleNode?["last_updated_on"]?.GetValue<string>();
            if (last_updated_on_node != null && last_updated_on_node.Length == 10)
            {
                reportingEntity.LastUpdatedOn = DateOnly.Parse(last_updated_on_node);
            }
            return reportingEntity;
        }

        public async Task ParseStream(ITargetBlock<Provider> providerTarget, ITargetBlock<Rate> rateTarget, FileInfo fileInfo)
        {
            if (fileInfo.HasProvidersFile)
            {
                await using var providerStream = File.OpenRead(fileInfo.ProviderProcessingPath);
                IAsyncEnumerable<JsonNode?> nodes = JsonSerializer.DeserializeAsyncEnumerable<JsonNode>(providerStream);
                try
                {
                    await foreach (var node in nodes)
                    {
                        var id = node["provider_group_id"]?.ToString();
                        if (id != null)
                        {
                            var group = providerParser.ParseProviderGroup(node);
                            foreach (var provider in group)
                            {
                                provider.ProviderReference = id.Truncate(20);
                                providerTarget.Post(provider);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Failed Parsing Providers");
                }
                logger.LogInformation("Finished Parsing Provider Section");
            }
            if (fileInfo.HasRatesFile)
            {
                await using FileStream file = File.OpenRead(fileInfo.RateProcessingPath);
                IAsyncEnumerable<JsonNode?> enumerable = JsonSerializer.DeserializeAsyncEnumerable<JsonNode>(file);
                try
                {
                    await foreach (JsonNode node in enumerable)
                    {
                        var rs = rateParser.ParseRatesForCode(node);
                        foreach (Rate r in rs.rates)
                            rateTarget.Post(r);
                        foreach (Provider p in rs.providers)
                            providerTarget.Post(p);
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Failed Parsing Rates");
                }
                logger.LogInformation("Finished Parsing Rates");
            }
            rateTarget.Complete();
            providerTarget.Complete();
        }
    }
}
