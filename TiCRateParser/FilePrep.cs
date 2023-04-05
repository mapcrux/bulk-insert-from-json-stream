using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiCRateParser
{
    public class FileInfo
    {
        public bool HasRatesFile { get; set; }
        public bool HasProvidersFile { get; set; }

        public string RateProcessingPath { get; set;}
        public string ProviderProcessingPath { get; set; }
        public string Preamble { get; set; }
        public FileInfo()
        {
            RateProcessingPath = "";
            ProviderProcessingPath = "";
            HasRatesFile = false;
            HasProvidersFile = false;
            Preamble = "";
        }
    }

    public class FilePrep : IDisposable
    {

        private string rateProcessingPath = "./processingRates.json";
        private string providerProcessingPath = "./processingProviders.json";
        private Stream source;

        public FilePrep(Stream s)
        {
            this.source = s;
        }

        public FilePrep(Stream s, string rateProcessingPath, string providerProcessingPath)
        {
            this.source = s;
            this.rateProcessingPath = rateProcessingPath;
            this.providerProcessingPath = providerProcessingPath;
        }

        public async Task<FileInfo> PrepareFile(int bufferSize = 4096)
        {
            using StreamReader sr = new StreamReader(source);
            var buffer = new char[bufferSize];
            FileInfo fileInfo = new FileInfo();
            var sb = new StringBuilder();
            string totalString = "";
            int current = 0;
            bool toRead = true;
            int currentBufferSize = 0;
            string currentString = "";
            while (!sr.EndOfStream || !toRead) {
                if (toRead) {
                    currentBufferSize = sr.ReadBlock(buffer);
                    currentString = new string(buffer).Substring(current, currentBufferSize-current);
                    totalString += currentString;
                }
                else
                {
                    currentString = new string(buffer).Substring(current, currentBufferSize-current);
                    totalString += currentString;
                }
                int findIndexRates = totalString.IndexOf("\"in_network\":");
                int findIndexProviders = totalString.IndexOf("\"provider_references\":");
                if (findIndexRates != -1 && (findIndexRates < findIndexProviders || findIndexProviders < 0))
                {
                    var ratesIndex = currentBufferSize - (totalString.Length - (findIndexRates + 13));
                    sb.Append(currentString.Substring(0, ratesIndex - current));
                    sb.Append("\"\"");
                    fileInfo.HasRatesFile = true;
                    fileInfo.RateProcessingPath = rateProcessingPath;
                    var writestream = File.Create(rateProcessingPath);
                    using StreamWriter streamWriter = new StreamWriter(writestream);
                    current = ratesIndex;
                    totalString = "";
                    TakeStreamToEndingArray(sr, streamWriter, buffer, ref current, ref currentBufferSize, bufferSize);
                    streamWriter.Flush();
                    streamWriter.Close();
                    toRead = false;
                    continue;
                }
                else if (findIndexProviders != -1 && (findIndexProviders < findIndexRates || findIndexRates < 0))
                {

                    var providersIndex = currentBufferSize - (totalString.Length - (findIndexProviders + 22));
                    sb.Append(currentString.Substring(0, providersIndex - current));
                    sb.Append("\"\"");
                    fileInfo.HasProvidersFile = true;
                    fileInfo.ProviderProcessingPath = providerProcessingPath;
                    using var writestream = File.Create(providerProcessingPath);
                    using StreamWriter streamWriter = new StreamWriter(writestream);
                    current = providersIndex;
                    totalString = "";
                    TakeStreamToEndingArray(sr, streamWriter, buffer, ref current, ref currentBufferSize, bufferSize);
                    streamWriter.Flush();
                    streamWriter.Close();
                    toRead = false;
                    continue;
                }
                else
                {
                    sb.Append(currentString);
                    toRead = true;
                }
            }
            fileInfo.Preamble = sb.ToString();
            return fileInfo;

        }

        private void TakeStreamToEndingArray(StreamReader sr, StreamWriter destination, char[] buffer, ref int current, ref int currentBufferSize, int bufferSize = 4096)
        {
            int arraystack = 0;
            bool isOpen = false;
            bool toRead = false;
            while (!sr.EndOfStream || !toRead)
            {
                if (toRead)
                {
                    current = 0;
                    currentBufferSize = sr.ReadBlock(buffer);
                }
                var start = current;
                while(current < currentBufferSize)
                {
                    if(buffer[current] == '[')
                    {
                        arraystack++;
                        isOpen = true;
                    }
                    else if(buffer[current] == ']')
                    {
                        arraystack--;
                        if (arraystack == 0 && isOpen) {
                            destination.Write(buffer, start, current++ + 1 - start);
                            return;
                        }
                    }
                    current++;
                }
                destination.Write(buffer, start, buffer.Length-start);
                toRead = true;
            }
            return;
        }

        public void Dispose()
        {
            source.Dispose();
        }
    }
}
