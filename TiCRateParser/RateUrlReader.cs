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
    public class RateUrlReader : IRateReader
    {
        private HttpClient client;

        public RateUrlReader()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Chrome/109.0.0.0");
        }


        public async Task<JsonNode> ReadFile(string path)
        {
            try
            {
                using (HttpResponseMessage response = await client.GetAsync(path))
                using (Stream stream = await response.Content.ReadAsStreamAsync())
                {
                    using var decompressor = new GZipStream(stream, CompressionMode.Decompress);
                    return JsonNode.Parse(decompressor, new JsonNodeOptions { }, new JsonDocumentOptions { AllowTrailingCommas = true });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error encountered while downloading,unzipping or parsing file: {e}");
                return null;
            }
        }
    }
}
