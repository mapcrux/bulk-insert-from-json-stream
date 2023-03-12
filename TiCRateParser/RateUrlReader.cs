using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace TiCRateParser
{
    public interface IRateUrlReader
    {
        Task<JsonTextReader> DownloadFile(string url);
    }
    public class RateUrlReader : IRateUrlReader
    {
        private HttpClient client;
        private ILogger logger;

        public RateUrlReader(ILogger<RateUrlReader> logger)
        {
            this.logger = logger;
            client = new HttpClient();
            client.Timeout = new TimeSpan(12, 0, 0);
            client.DefaultRequestHeaders.Add("User-Agent", "Chrome/109.0.0.0");
        }


        public async Task<JsonTextReader> DownloadFile(string url)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                Stream stream = await response.Content.ReadAsStreamAsync();
                var decompressor = new GZipStream(stream, CompressionMode.Decompress);
                StreamReader streamReader = new StreamReader(decompressor);
                return new JsonTextReader(streamReader);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error encountered while downloading or unzipping url");
                return null;
            }
        }
    }
}
