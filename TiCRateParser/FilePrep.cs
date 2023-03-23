using System;
using System.Collections.Generic;
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

        public async Task<FileInfo> PrepareFile(int bufferSize = 50)
        {
            FileInfo fileInfo = new FileInfo();
            Queue<byte> chars = new Queue<byte>();
            List<byte> bytes = new List<byte>();
            int current;
            while ((current = source.ReadByte()) != -1)
            {
                byte b = (byte)current;
                if (chars.Count >= bufferSize) chars.Dequeue();
                chars.Enqueue(b);
                bytes.Add(b);
                var bufferText = Encoding.UTF8.GetString(bytes.ToArray());
                if (!string.IsNullOrEmpty(bufferText) && bufferText.EndsWithIgnoreCaseAndWhiteSpace("\"in_network\":"))
                {
                    fileInfo.HasRatesFile = true;
                    fileInfo.RateProcessingPath = rateProcessingPath;
                    var writestream = File.Create(rateProcessingPath);
                    await TakeStreamToEndingArray(writestream);
                    bytes.AddRange(Encoding.UTF8.GetBytes("[]"));
                }
                else if (!string.IsNullOrEmpty(bufferText) && bufferText.EndsWithIgnoreCaseAndWhiteSpace("\"provider_references\":"))
                {
                    fileInfo.HasProvidersFile = true;
                    fileInfo.ProviderProcessingPath = providerProcessingPath;
                    var writestream = File.Create(providerProcessingPath);
                    await TakeStreamToEndingArray(writestream);
                    bytes.AddRange(Encoding.UTF8.GetBytes("[]"));
                }
            }
            fileInfo.Preamble = Encoding.UTF8.GetString(bytes.ToArray(), 0, bytes.Count);
            return fileInfo;

        }

        private async Task TakeStreamToEndingArray(Stream destination, int bufferSize = 4096)
        {
            Queue<byte> bytes = new Queue<byte>();
            int arraystack = 0;
            int current;
            List<byte> buffer = new List<byte>();
            bool isOpen = true;
            while ((current = source.ReadByte()) != -1 && (arraystack > 0 || isOpen))
            {
                byte b = (byte)current;
                if (bytes.Count >= 4) bytes.Dequeue();
                bytes.Enqueue(b);
                buffer.Add(b);
                var s = Encoding.UTF8.GetString(bytes.ToArray());
                if (s.EndsWith("["))
                {
                    arraystack++;
                    isOpen = false;
                }
                if (s.EndsWith("]"))
                {
                    arraystack--;
                    if (arraystack == 0) break;
                }
                if (buffer.Count >= bufferSize)
                {
                    await destination.WriteAsync(buffer.ToArray(), 0, buffer.Count);
                    buffer.Clear();
                }
            }
            await destination.WriteAsync(buffer.ToArray(), 0, buffer.Count);
            buffer.Clear();
            await destination.FlushAsync();
        }

        public void Dispose()
        {
            source.Dispose();
        }
    }
}
