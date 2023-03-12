using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TiCRateParser
{
    public interface IRateService
    {
        Task<int> ImportUrlsGzip(string[] urls);
        Task<int> ImportFiles(string[] filePaths);
        Task<string[]> ParseIndex(string indexUrl);
    }

    public class RateService : IRateService
    {
        private readonly ILogger logger;
        private IRateUrlReader urlReader;
        private IRateFileReader fileReader;
        private IDatabaseInsert databaseInsert;
        private IRateReader rateReader;

        public RateService(ILogger<RateService> logger, IRateUrlReader rateUrlReader, IRateFileReader fileReader, IDatabaseInsert databaseInsert, IRateReader rateReader)
        {
            this.logger = logger;
            this.urlReader = rateUrlReader;
            this.fileReader = fileReader;
            this.databaseInsert = databaseInsert;
            this.rateReader = rateReader;
        }

        public async Task<int> ImportFiles(string[] filePaths)
        {
            int totalCount = 0;
            foreach (var file in filePaths)
            {
                //Read File
                try
                {
                    using JsonTextReader jsonReader = await fileReader.ReadFile(file);
                    int rateCount = await ImportRatesFromStream(jsonReader);
                    logger.LogInformation($"Inserted {rateCount} rates for file: {file}");
                    totalCount += rateCount;
                }
                catch(Exception e)
                {
                    logger.LogError(e, $"Unhandled exception in parsing {file}");
                }
            }
            return totalCount;
        }

        private async Task<int> ImportRatesFromStream(JsonTextReader jsonReader)
        {
            int rateCount = 0;
            // Rate Pipeline Setup
            var rateBuffer = new BatchBlock<Rate>(50000);
            var rateConsumer = new ActionBlock<Rate[]>(rateBatch =>
            {
                databaseInsert.InsertRates(rateBatch);
                Interlocked.Add(ref rateCount, rateBatch.Length);
            });
            rateBuffer.LinkTo(rateConsumer);
            var rateCompletion = rateBuffer.Completion.ContinueWith(delegate { rateConsumer.Complete(); });
            // Provider Pipeline Setup
            databaseInsert.TruncateProviderStage();
            var providerBuffer = new BatchBlock<Provider>(50000);
            var providerConsumer = new ActionBlock<Provider[]>(providerBatch =>
            {
                databaseInsert.InsertProviderSection(providerBatch);
            });
            providerBuffer.LinkTo(providerConsumer);
            var providerCompletion = providerBuffer.Completion.ContinueWith(delegate { providerConsumer.Complete(); });
            //Start Parsing
            var reportingEntity = await rateReader.ParseStream(jsonReader, providerBuffer, rateBuffer);

            providerBuffer.Complete();
            rateBuffer.Complete();
            Task.WaitAll(rateCompletion, rateConsumer.Completion, providerCompletion, providerConsumer.Completion);
            databaseInsert.CopyProvidersFromStage();
            //Insert reporting entity
            databaseInsert.InsertReportingEntity(reportingEntity);
            return rateCount;
        }

        public async Task<int> ImportUrlsGzip(string[] urls)
        {
            int totalCount = 0;
            foreach (var url in urls)
            {
                //Read File
                try
                {
                    using JsonTextReader jsonReader = await urlReader.DownloadFile(url);
                    int rateCount = await ImportRatesFromStream(jsonReader);
                    logger.LogInformation($"Inserted {rateCount} rates for file: {url}");
                    totalCount += rateCount;
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Unhandled exception in parsing {url}");
                }
            }
            return totalCount;
        }

        public async Task<string[]> ParseIndex(string indexUrl)
        {
            throw new NotImplementedException();
        }
    }

}
