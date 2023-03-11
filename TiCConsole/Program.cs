using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using TiCRateParser;

namespace TiCConsole
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<ILoggerFactory, LoggerFactory>();
                    services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
                    services.AddHostedService<ConsoleHostedService>();
                    services.AddSingleton<IRateUrlReader, RateUrlReader>();
                    services.AddSingleton<IRateFileReader, RateFileReader>();
                    services.AddSingleton<IRateReader, RateReader>();
                    services.AddSingleton<IRateService, RateService>();
                    services.AddSingleton<IProviderParser, ProviderParser>();
                    services.AddSingleton<IRateParser, RateParser>();
                    services.AddSingleton<IDatabaseInsert>(x => new DatabaseInsert(
                        x.GetService<ILogger<DatabaseInsert>>(),
                        "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Rates;Integrated Security=True"
                        ));
                })
                .ConfigureLogging((_, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddSimpleConsole(options => options.IncludeScopes = true);
                })
                .RunConsoleAsync();
        }
    }

    public sealed class ConsoleHostedService : IHostedService
    {
        private readonly ILogger logger;
        private readonly IHostApplicationLifetime appLifetime;
        private readonly IRateService rateService;

        public ConsoleHostedService(
            ILogger<ConsoleHostedService> logger,
            IHostApplicationLifetime appLifetime,
            IRateService rateService)
        {
            this.logger = logger;
            this.appLifetime = appLifetime;
            this.rateService = rateService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogDebug($"Starting Execution: {string.Join(" ", Environment.GetCommandLineArgs())}");

            appLifetime.ApplicationStarted.Register(() =>
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await rateService.ImportFiles(new string[] { 
                            @"c:\temp\2023-02-01_United-HealthCare-Services--Inc-_Third-Party-Administrator_EP1-50_C1_in-network-rates.json" 
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Unhandled exception");
                    }
                    finally
                    {
                        // Stop the application once the work is done
                        appLifetime.StopApplication();
                    }
                });
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
