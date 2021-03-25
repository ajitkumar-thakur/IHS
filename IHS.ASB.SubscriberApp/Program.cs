using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using IHS.ASB.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace IHS.ASB.SubscriberApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            TelemetryClient telemetryClient;
            var host = CreateHostBuilder(args).Build();
            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;
                ILogger<Program> logger = services.GetRequiredService<ILogger<Program>>();
                telemetryClient = services.GetRequiredService<TelemetryClient>();
                try
                {
                    using (telemetryClient.StartOperation<RequestTelemetry>("Execute Async"))
                    {
                        logger.LogInformation("Program started");
                        var myService = services.GetRequiredService<Application>();
                        await myService.Run();

                        logger.LogInformation("Program closed");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogCritical("Application failed to Start {Details}", ex);
                }
            }
            // Explicitly calling Flush() followed by sleep is required in Console Apps.
            // This is to ensure that even if the application terminates, telemetry is sent to the back end.
            telemetryClient.Flush();
            Thread.Sleep(1000);
            Console.ReadKey();
        }
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                                   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                   .Build();

            var serilogger = new LoggerConfiguration()
                                    .ReadFrom.Configuration(configuration)
                                    // .WriteTo
                                    // .ApplicationInsights(TelemetryConfiguration.Active, TelemetryConverter.Traces)
                                    .CreateLogger();

            Log.Logger = serilogger;

            Log.Information("Host builder Started");
            try
            {
                return new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddConfiguration(configuration);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddSerilog();
                    // logging.AddApplicationInsights();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<ISubscriptionRepository, SubscriptionRepository>();
                    // services.AddTransient<IMessageRepository, MockMessageRepository>();
                    services.AddTransient<Application>();
                    services.AddApplicationInsightsTelemetryWorkerService();
                });
            }
            catch (Exception ex)
            {
                Log.Fatal("Failed to build Host {Details}", ex);
                throw;
            }
            finally
            {
                Log.Information("Host build Completed");
            }
        }
    }
}
