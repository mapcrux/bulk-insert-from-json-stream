using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading.Tasks.Dataflow;

namespace TiCRateParser
{
    public interface IRateFileReader
    {
        Task<JsonTextReader> ReadFile(string path);
    }

    public class RateFileReader : IRateFileReader
    {
        private readonly ILogger logger;
        public RateFileReader(ILogger<RateReader> logger)
        {
            this.logger = logger;
        }

        public async Task<JsonTextReader> ReadFile(string path)
        {
            try
            {
                FileStream fileread = File.OpenRead(path);
                StreamReader streamReader = new StreamReader(fileread);
                return new JsonTextReader(streamReader);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error encountered while reading file");
                return null;
            }
        }
    }
}
