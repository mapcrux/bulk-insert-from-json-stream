using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TiCRateParser
{
    public interface IRateService
    {
        //Task<int> ImportUrlsGzip(string[] urls);
        Task<int> ImportFiles(string[] filePaths);
        Task<string[]> ParseIndex(string indexUrl);
    }

    public class RateService : IRateService
    {
        private readonly ILogger logger;
        private IDatabaseInsert databaseInsert;
        private IRateReader rateReader;

        public RateService(ILogger<RateService> logger, IDatabaseInsert databaseInsert, IRateReader rateReader)
        {
            this.logger = logger;
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
                    using var fileStream = File.OpenRead(file);
                    using var fp = new FilePrep(fileStream);
                    logger.LogInformation($"Dividing File");
                    var fileInfo = await fp.PrepareFile();
                    var reportingEntity = rateReader.ParsePreamble(fileInfo);
                    databaseInsert.InsertReportingEntity(reportingEntity);
                    int rateCount = await ImportRatesFromStream(fileInfo, reportingEntity);
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

        private async Task<int> ImportRatesFromStream(FileInfo fileInfo, ReportingEntity reportingEntity)
        {
            int rateCount = 0;
            // Rate Pipeline Setup
            var rateBuffer = new BatchBlock<Rate>(50000);
            var rateConsumer = new ActionBlock<Rate[]>(rateBatch =>
            {
                databaseInsert.InsertRates(rateBatch, reportingEntity.Id);
                Interlocked.Add(ref rateCount, rateBatch.Length);
            });
            rateBuffer.LinkTo(rateConsumer);
            var rateCompletion = rateBuffer.Completion.ContinueWith(delegate { rateConsumer.Complete(); });
            // Provider Pipeline Setup
            databaseInsert.TruncateStage();
            var providerBuffer = new BatchBlock<Provider>(50000);
            var providerConsumer = new ActionBlock<Provider[]>(providerBatch =>
            {
                databaseInsert.InsertProviderSection(providerBatch);
            });
            providerBuffer.LinkTo(providerConsumer);
            var providerCompletion = providerBuffer.Completion.ContinueWith(delegate { providerConsumer.Complete(); });
            //Start Parsing
            await rateReader.ParseStream(providerBuffer, rateBuffer, fileInfo);
            providerBuffer.Complete();
            rateBuffer.Complete();
            Task.WaitAll(rateCompletion, rateConsumer.Completion, providerCompletion, providerConsumer.Completion);
            databaseInsert.CopyFromStage();
            return rateCount;
        }

        //public async Task<int> ImportUrlsGzip(string[] urls)
        //{
        //    int totalCount = 0;
        //    foreach (var url in urls)
        //    {
        //        //Read File
        //        try
        //        {
        //            using Newtonsoft.Json.JsonTextReader jsonReader = await urlReader.DownloadFile(url);
        //            int rateCount = await ImportRatesFromStream(jsonReader);
        //            logger.LogInformation($"Inserted {rateCount} rates for file: {url}");
        //            totalCount += rateCount;
        //        }
        //        catch (Exception e)
        //        {
        //            logger.LogError(e, $"Unhandled exception in parsing {url}");
        //        }
        //    }
        //    return totalCount;
        //}

        public async Task<string[]> ParseIndex(string indexUrl)
        {
            throw new NotImplementedException();
        }
    }

}
