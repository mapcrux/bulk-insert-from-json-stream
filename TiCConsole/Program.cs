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
                            @"C:\temp\2023-01-16_378_49A0_in-network-rates_36_of_84.json"
                        });
                        //await rateService.ImportUrlsGzip(new string[]
                        //{
                        //    @"https://antm-pt-prod-dataz-nogbd-nophi-us-east1.s3.amazonaws.com/anthem/OH_BCCMMEDCL00_17B0.json.gz"
                        //    @"https://antm-pt-prod-dataz-nogbd-nophi-us-east1.s3.amazonaws.com/anthem/OH_CACHMED0000.json.gz"
                        //    @"https://anthembcbsga.mrf.bcbs.com/2023-02_510_01B0_in-network-rates_28_of_29.json.gz?&Expires=1679603545&Signature=bVYh7actTMpOWJ-m0oc3gOR1WQQQKmsv-lIickn-wMcxn7e5kaAF9-pDYHW7ygu4GfxD-rSD2-5p6ZzWPUSYl-xZkt8dylN1KyJzcnK3owt~3sCLHhA7QhPpC3Xk3AUc6RiRhrp~8dhafXsP11pW~lwrZB3F-UlbA664uReBp1pQH4O4Dxvr3KMtrRKkK5TQmdINGcJcy7579SGp51lFMtvSeDTjXcX1A8zMn0EivJh45~YLRagqcF5ChwtMR6dtHKtJPQY1I3sTCIK2a6dA0XUZ5QYjUAk7fLfxErZLNR3QY9lXtMAeM3w6uN7EmZhe07ehQ7rcR5sOEkLAMr6IfQ__&Key-Pair-Id=K27TQMT39R1C8A"
                        //});
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
