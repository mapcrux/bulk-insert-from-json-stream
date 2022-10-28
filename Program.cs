using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ConsoleApp3
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
                buffer.Completion.ContinueWith(delegate { consumerTask.Complete(); });
                await JsonParser.Produce(buffer);
                buffer.Complete();

                consumerTask.Completion.Wait();
                Console.ReadLine();
            }
            catch {
                Console.ReadLine();
            }
        }
    }
}
