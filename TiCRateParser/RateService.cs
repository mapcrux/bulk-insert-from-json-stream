using Microsoft.Extensions.Logging;
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
        //public async ParseUrls()
        //{

        //}
        //public static async Task Main(string[] args)
        //{
        //    try
        //    {
        //        string[] urlsToParse = new string[] {
        //            "https://anthembcbsga.mrf.bcbs.com/2023-02_378_49A0_in-network-rates_36_of_84.json.gz?&Expires=1679603545&Signature=Wdfp1q18z7YmxQ0eskWExeXMtxtTTWZTFOeO3ZlMkE8usYQImKlqNqOYcwlxMA9pApn5K-D5IjWqekzppIhWvE9dee5SnPIgCTcb814an-OzHOexmZMbUKlUqAFgOW0q9kU72Mstko5sm0nqMETZQcgks7bgNisc11sKceHCLGT6OojHId7-M8TDXpKKIGLqvm5tAk6L36fnXIaDvDbWKBevA3WljSYtfA-mxN3Reid1TxeqIYjqj98jpGmjmPxQqDKe919iGgx1a-cWHyZBlz8oxK3BfJjP6acJlzthFWmPwG2u4bZGDjWz2wAhjlOP-e8SWF~0pKYG6j2dVCpKCg__&Key-Pair-Id=K27TQMT39R1C8A"
        //        };
        //        foreach (string url in urlsToParse)
        //        {
        //            var urlReader = new RateUrlReader();
        //            var node = await urlReader.ReadFile(url);


        //            //var fileReader = new RateFileReader();
        //            //var node = await fileReader.ReadFile(@"C:\temp\2023-02-01_United-HealthCare-Services--Inc-_Third-Party-Administrator_EP1-50_C1_in-network-rates.json");
        //            IRateParser parser = new RateParser(node);
        //            DatabaseInsert databaseInsert = new DatabaseInsert("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Rates;Integrated Security=True");


        //            var entityId = databaseInsert.InsertReportingEntity(parser.reporting_entity_name, parser.reporting_entity_type, parser.last_updated_on);
        //            //if (fileReader.streamingParserRequired)
        //            //{
        //            //    databaseInsert.InsertProviderSection(parser.providers);

        //            //}
        //            //else
        //            //{
        //            var rates = parser.ParseRates();
        //            databaseInsert.InsertProviderSection(parser.providers);
        //            databaseInsert.InsertRates(rates.ToArray(), entityId);
        //            Console.WriteLine("Finished Writing Rates to DB");
        //            //}

        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.Message);
        //        Console.ReadLine();
        //    }
        //}
        private readonly ILogger logger;
        private IRateUrlReader urlReader;
        private IRateFileReader fileReader;
        private IDatabaseInsert databaseInsert;

        public RateService(ILogger<RateService> logger, IRateUrlReader rateUrlReader, IRateFileReader fileReader, IDatabaseInsert databaseInsert)
        {
            this.logger = logger;
            this.urlReader = rateUrlReader;
            this.fileReader = fileReader;
            this.databaseInsert = databaseInsert;
        }

        public async Task<int> ImportFiles(string[] filePaths)
        {
            int totalCount = 0;
            foreach(var file in filePaths)
            {
                
                var providerBuffer = new BatchBlock<Provider>(50000);
                var rateBuffer = new BatchBlock<Rate>(50000);
                int rateCount = 0;
                //var consumerTask = new ActionBlock<Rate[]>(rateBatch =>
                //{
                //databaseInsert.InsertRates(rateBatch, entityId)
                //    Interlocked.Add(ref COUNTER, rateBatch.length);
                //});
                //buffer.LinkTo(consumerTask);
                //var completion = providerBuffer.Completion.ContinueWith(delegate { consumerTask.Complete(); });
                await fileReader.ReadFile(file, providerBuffer, rateBuffer);
                providerBuffer.Complete();
                rateBuffer.Complete();
                //Task.WaitAll(completion, consumerTask.Completion);
                logger.LogInformation($"Inserted {rateCount} rates for file: {file}");
                totalCount += rateCount;
            }
            return totalCount;
        }

        public async Task<int> ImportUrlsGzip(string[] urls)
        {
            throw new NotImplementedException();
        }

        public async Task<string[]> ParseIndex(string indexUrl)
        {
            throw new NotImplementedException();
        }
    }

}
