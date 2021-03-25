using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IHS.ASB.Core;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
namespace IHS.ASB.PublisherApp
{
    class Program
    {

        static async Task Main(string[] args)
        {
            // TelemetryClient telemetryClient;
            // TelemetryConfiguration telemetryConfiguration;
            // IConfiguration _configuration;
            var host = CreateHostBuilder(args).Build();
            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;
                ILogger<Program> logger = services.GetRequiredService<ILogger<Program>>();
                // telemetryClient = services.GetRequiredService<TelemetryClient>();
                try
                {
                    // telemetryConfiguration = services.GetRequiredService<TelemetryConfiguration>();
                    // _configuration = services.GetRequiredService<IConfiguration>();
                    // telemetryConfiguration.TelemetryChannel.DeveloperMode = _configuration.GetValue<bool>("TelemetryDeveloperMode");
                    // telemetryClient.TrackTrace("Telemetry Started");

                    // using (telemetryClient.StartOperation<RequestTelemetry>("Publisher Application"))
                    // {
                    logger.LogInformation("Program started");
                    var myService = services.GetRequiredService<Application>();
                    await myService.Run();
                    logger.LogInformation("Program closed");
                    // }
                }
                catch (Exception ex)
                {
                    logger.LogCritical("Application failed to Start {Details}", ex);
                }
            }
            // Explicitly calling Flush() followed by sleep is required in Console Apps.
            // This is to ensure that even if the application terminates, telemetry is sent to the back end.
            // if (telemetryClient != null)
            // {
            //     telemetryClient.Flush();
            // }
            Thread.Sleep(5000);
        }
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                                   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                   .Build();

            // var telemetryConfig = TelemetryConfiguration.CreateDefault();
            // telemetryConfig.TelemetryChannel.DeveloperMode = configuration.GetValue<bool>("TelemetryDeveloperMode");
            // telemetryConfig.InstrumentationKey = configuration.GetSection("ApplicationInsights:InstrumentationKey").Value;

            var serilogger = new LoggerConfiguration()
                                    .ReadFrom.Configuration(configuration)
                                    // .WriteTo
                                    // .ApplicationInsights(telemetryConfig, TelemetryConverter.Traces)
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
                    // logging.AddConsole();
                    logging.AddSerilog();
                    // logging.AddApplicationInsights();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<IMessageRepository, MessageRepository>();
                    // services.AddTransient<IMessageRepository, MockMessageRepository>();
                    // services.AddApplicationInsightsTelemetryWorkerService();
                    // TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = true
                    services.AddTransient<Application>();
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
