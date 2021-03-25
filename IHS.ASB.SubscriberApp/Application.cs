using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using IHS.ASB.Core;
using System.Threading.Tasks;
using System;

namespace IHS.ASB.SubscriberApp
{
    public class Application
    {
        private readonly ILogger _logger;
        private readonly ISubscriptionRepository _subscriptionRepository;

        public Application(ILoggerFactory factory,
        ISubscriptionRepository subscriptionRepository
        )
        {
            _logger = factory.CreateLogger("Subsciber Application");
            _subscriptionRepository = subscriptionRepository;
        }
        internal async Task Run()
        {
            _logger.LogInformation("Application Started");
            try
            {
                await _subscriptionRepository.Subscribe();
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error: {Detail}", ex);
            }
            finally
            {
                _logger.LogInformation("Application Ended");
            }
            await Task.FromResult($"Application Ended at {DateTime.UtcNow}");
        }
    }
}