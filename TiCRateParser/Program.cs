using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TiCRateParser
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var rateParser = new UnitedRateParser(@"C:\temp\2023-02-01_United-HealthCare-Services--Inc-_Third-Party-Administrator_EP1-50_C1_in-network-rates.json");
                await rateParser.FilePrep();

                var providers = rateParser.providers.Select(x => x.Value);
                var providersTable = Database.ProvidersToDataTable(providers);
                Database.BulkInsert(providersTable, "ProviderStage");
                var TINTable = Database.TINToDataTable(providers);
                Database.BulkInsert(TINTable, "TINStage");
                var npiTable = Database.NPIToDataTable(providers);
                Database.BulkInsert(npiTable, "NPIStage");
                var entityId = Database.InsertReportingEntity(rateParser.reporting_entity_name, rateParser.reporting_entity_type, rateParser.last_updated_on);
                var buffer = new BatchBlock<Rate>(50000);
                var consumerTask = new ActionBlock<Rate[]>(a =>
                {
                    Console.WriteLine($"Inserting batch of {a.Length} Rates");
                    var table = Database.RatesToDataTable(a, entityId);
                    Database.BulkInsert(table, "Rates");
                });
                buffer.LinkTo(consumerTask);
                var completion = buffer.Completion.ContinueWith(delegate { consumerTask.Complete(); });
                await rateParser.Produce(buffer);
                buffer.Complete();
                Task.WaitAll(completion, consumerTask.Completion);
                Console.WriteLine("Finished Writing Rates to DB");
            }
            catch (Exception e){
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }
    }
}
