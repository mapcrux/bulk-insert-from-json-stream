using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace TiCRateParser
{
    public class RateFileReader : IRateReader
    {
        public string processingPath { get; set; }
        public bool streamingParserRequired { get; set; }

        public RateFileReader()
        {
            this.processingPath = "./processing.json";
        }

        public RateFileReader(string processingPath)
        {
            this.processingPath = processingPath;
        }

        public async Task<JsonNode> ReadFile(string path)
        {
            try
            {
                FileInfo fi = new FileInfo(path);
                if (fi.Length > 1073741824)
                {
                    streamingParserRequired = true;
                    return await FilePrep(path);
                }
                else
                {
                    streamingParserRequired = false;
                    await using FileStream fileread = File.OpenRead(path);
                    return JsonNode.Parse(fileread, new JsonNodeOptions { }, new JsonDocumentOptions { AllowTrailingCommas = true });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error encountered while reading and parsing file: {e}");
                return null;
            }
        }

        private async Task<JsonNode> FilePrep(string path)
        {

            string preamble = "";
            await using FileStream fileread = File.OpenRead(path);
            await using FileStream filewrite = File.Create(processingPath);
            using (StreamReader reader = new StreamReader(fileread))
            {
                using (StreamWriter writer = new StreamWriter(filewrite))
                {
                    string? line = reader.ReadLine();
                    while (!line.TrimStart().StartsWith("\"in_network\""))
                    {
                        preamble += line;
                        line = reader.ReadLine();
                    }
                    preamble += "}";

                    writer.WriteLine("[");
                    do
                    {
                        line = reader.ReadLine();
                        if (reader.Peek() != -1)
                        {
                            writer.WriteLine(line);
                        }

                    } while (line != null);
                }
            }
            var preambleNode = JsonNode.Parse(preamble, new JsonNodeOptions { }, new JsonDocumentOptions { AllowTrailingCommas = true });
            return preambleNode;
        }
    }
}
