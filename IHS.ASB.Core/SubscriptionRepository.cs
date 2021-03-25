using System;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IHS.ASB.Core.Models;
using IHS.ASB.DataAccess;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IHS.ASB.Core
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        private SubscriptionMesssage _subscriptionMesssage;

        private ISubscriptionClient _subscriptionClient;

        public SubscriptionRepository(IConfiguration Config, ILoggerFactory factory)
        {
            _config = Config;
            _logger = factory.CreateLogger("Subscription Repository");
            _connectionString = _config.GetConnectionString("IHSASBConnection");
        }

        private SubscriptionMesssage GetSubscriptions()
        {
            SubscriptionMesssage subscription = new SubscriptionMesssage();
            System.Data.DataSet ds = SqlHelper.ExecuteDataset(_connectionString, "ASBMessaging_Subscriptions_Get_Details");
            if (ds != null && ds.Tables.Count > 0)
            {
                if (ds.Tables[0].Rows.Count > 0)
                {
                    DataRow dr = ds.Tables[0].Rows[0];
                    subscription.Id = dr.Field<int>("ID");
                    subscription.TopicName = dr["TopicName"].ToString();
                    subscription.SubscriptionName = dr["SubscriptionName"].ToString();
                    subscription.Description = dr["Description"].ToString();
                    subscription.TypeOfTopic = dr["Description"].ToString();
                    subscription.PrimaryKey = dr["PrimaryKey"].ToString();
                    subscription.PrimaryConnectionString = dr["PrimaryConnectionString"].ToString();
                    subscription.SecondaryKey = dr["SecondaryKey"].ToString();
                    subscription.SecondaryConnectionString = dr["SecondaryConnectionString"].ToString();
                    subscription.IsEnabled = dr.Field<bool?>("IsEnabled");
                    subscription.CreatedDate = dr.Field<DateTime?>("CreatedDate");
                }
            }
            return subscription;
        }

        private void RegisterOnMessageHandlerAndReceiveMessages()
        {
            // Configure the message handler options in terms of exception handling, number of concurrent messages to deliver, etc.
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of concurrent calls to the callback ProcessMessagesAsync(), set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 1,

                // Indicates whether the message pump should automatically complete the messages after returning from user callback.
                // False below indicates the complete operation is handled by the user callback as in ProcessMessagesAsync().
                AutoComplete = false
            };

            // Register the function that processes messages.
            _subscriptionClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
        }

        private async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            // Process the message.
            // Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");
            CreateMessage(_subscriptionMesssage.TopicName, _subscriptionMesssage.SubscriptionName, message.SystemProperties.SequenceNumber, Encoding.UTF8.GetString(message.Body));
            // Complete the message so that it is not received again.
            // This can be done only if the subscriptionClient is created in ReceiveMode.PeekLock mode (which is the default).
            await _subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);

            // Note: Use the cancellationToken passed as necessary to determine if the subscriptionClient has already been closed.
            // If subscriptionClient has already been closed, you can choose to not call CompleteAsync() or AbandonAsync() etc.
            // to avoid unnecessary exceptions.
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            _logger.LogError("Error - Message handler encountered an exception {Details}", exceptionReceivedEventArgs.Exception);
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            _logger.LogError("Exception context for troubleshooting:", context);
            return Task.CompletedTask;
        }

        private void CreateMessage(string topic, string subscription, long sequenceNumber, string message)
        {
            Convert.ToInt32(SqlHelper.ExecuteScalar(_connectionString, "ASBMessaging_Subscriptions_Insert_Message", topic, subscription, sequenceNumber, message));
        }

        public async Task Subscribe()
        {
            try
            {
                _subscriptionMesssage = GetSubscriptions();
                if (_subscriptionMesssage != null)
                {
                    _subscriptionClient = new SubscriptionClient(_subscriptionMesssage.PrimaryConnectionString
                    , _subscriptionMesssage.TopicName, _subscriptionMesssage.SubscriptionName);
                    // Console.WriteLine("======================================================");
                    // Console.WriteLine("Press ENTER key to exit after receiving all the messages.");
                    // Console.WriteLine("======================================================");

                    // Register subscription message handler and receive messages in a loop
                    RegisterOnMessageHandlerAndReceiveMessages();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error - Subscription {Details}", ex);
            }
            finally
            {
                Console.ReadKey();
                await _subscriptionClient.CloseAsync();
            }
        }
    }
}