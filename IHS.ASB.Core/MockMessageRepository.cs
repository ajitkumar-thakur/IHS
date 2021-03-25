using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IHS.ASB.Core
{
    public class MockMessageRepository : IMessageRepository
    {
        private readonly ILogger _logger;

        public MockMessageRepository(ILoggerFactory factory)
        {
            _logger = factory.CreateLogger("Publish Message");
        }
        public Task Publish()
        {
            try
            {
                throw new System.ArgumentException("Message Repository log", "original");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error - Reading Messages from DB {Details} ", ex);
                throw ex;
            }

        }
    }
}