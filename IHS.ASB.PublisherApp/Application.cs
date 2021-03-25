using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using IHS.ASB.Core;

namespace IHS.ASB.PublisherApp
{
    public class Application
    {
        private readonly ILogger _logger;
        private readonly IMessageRepository _messageRepository;
        public Application(ILoggerFactory factory,
        IMessageRepository messageRepository
        )
        {
            _logger = factory.CreateLogger("Publisher Application");
            _messageRepository = messageRepository;
        }
        internal async Task Run()
        {
            _logger.LogInformation("Application Started");
            try
            {
                await _messageRepository.Publish();
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