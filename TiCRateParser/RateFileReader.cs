using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading.Tasks.Dataflow;

namespace TiCRateParser
{
    public class RateFileReader : RateReader, IRateFileReader
    {
        private readonly ILogger logger;
        private readonly IProviderParser providerParser;
        private readonly IRateParser rateParser;

        public RateFileReader(ILogger<RateReader> logger, IProviderParser providerParser, IRateParser rateParser)
        {
            this.logger = logger;
            this.providerParser = providerParser;
            this.rateParser = rateParser;
        }

        public async Task<ReportingEntity> ReadFile(string path, ITargetBlock<Provider> providerTarget, ITargetBlock<Rate> rateTarget)
        {
            try
            {
                ReportingEntity entity = new ReportingEntity();
                await using FileStream fileread = File.OpenRead(path);
                using StreamReader streamReader = new StreamReader(fileread);
                using var jsonReader = new JsonTextReader(streamReader);
                await ParseStream(jsonReader, entity, providerTarget, rateTarget);
                return entity;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error encountered while reading file");
                return null;
            }
        }
    }
}
