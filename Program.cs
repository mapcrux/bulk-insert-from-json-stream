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
                var buffer = new BatchBlock<Rate>(10000);
                var consumerTask = new ActionBlock<Rate[]>(a =>
                    JsonParser.Consume(a));
                buffer.LinkTo(consumerTask);
                var completion = buffer.Completion.ContinueWith(delegate { consumerTask.Complete(); });
                await JsonParser.Produce(buffer);
                Console.WriteLine("Finished Parsing Json");
                buffer.Complete();
                Task.WaitAll(completion, consumerTask.Completion);
                Console.WriteLine("Finished Writing to DB");
            }
            catch (Exception e){
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }
    }
}
