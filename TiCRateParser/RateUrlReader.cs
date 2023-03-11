using Microsoft.Extensions.Logging;
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
    public class RateUrlReader : IRateUrlReader
    {
        private HttpClient client;
        private ILogger logger;

        public RateUrlReader(ILogger<RateUrlReader> logger)
        {
            this.logger = logger;
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Chrome/109.0.0.0");
        }


        //public async Task<JsonNode> DownloadFile(string path)
        //{
        //    try
        //    {
        //        using (HttpResponseMessage response = await client.GetAsync(path))
        //        using (Stream stream = await response.Content.ReadAsStreamAsync())
        //        {
        //            using (var decompressor = new GZipStream(stream, CompressionMode.Decompress))
        //            {
        //                using (StreamReader sr = new StreamReader(decompressor))
        //                {
                            
        //                }
        //            }
        //            return JsonNode.Parse(decompressor, new JsonNodeOptions { }, new JsonDocumentOptions { AllowTrailingCommas = true });
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        logger.LogError(e, "Error encountered while downloading,unzipping or parsing file");
        //        return null;
        //    }
        //}
    }
}
