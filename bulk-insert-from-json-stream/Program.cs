using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace BulkInsertFromJsonStream
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var providers = await JsonParser.ParseProviders();
                var providersTable = JsonParser.ProvidersToDataTable(providers);
                JsonParser.BulkInsert(providersTable, "Providers");
                var npiTable = JsonParser.NPIToDataTable(providers);
                JsonParser.BulkInsert(npiTable, "NPI");
                var buffer = new BatchBlock<Rate>(10000);
                var consumerTask = new ActionBlock<Rate[]>(a =>
                    JsonParser.Consume(a));
                buffer.LinkTo(consumerTask);
                var completion = buffer.Completion.ContinueWith(delegate { consumerTask.Complete(); });
                await JsonParser.Produce(buffer);
                buffer.Complete();
                Task.WaitAll(completion, consumerTask.Completion);
                Console.WriteLine("Finished Writing Rates to DB");
                Console.WriteLine("Beginning Writing Code Descriptions to DB");
                JsonParser.InsertCodes();
                Console.WriteLine("Finished Writing Code Descriptions to DB");
            }
            catch (Exception e){
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }
    }
}
