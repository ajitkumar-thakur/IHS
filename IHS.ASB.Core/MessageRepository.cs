using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using IHS.ASB.DataAccess;
using IHS.ASB.Core.Models;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using System.Text;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Linq;

namespace IHS.ASB.Core
{
    public class MessageRepository : IMessageRepository
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        private ITopicClient topicClient;

        public MessageRepository(IConfiguration Config, ILoggerFactory factory)
        {
            _config = Config;
            _logger = factory.CreateLogger("Publish Message");
            _connectionString = _config.GetConnectionString("IHSASBConnection");
        }

        private bool ValidateMessage(int id, string message, string schema)
        {
            JSchema jschema = null;
            bool isValid = false;
            try
            {
                jschema = JSchema.Parse(schema);

                if (jschema != null)
                {
                    JArray jobject = JArray.Parse(message);
                    IList<string> validationEvents = new List<string>();
                    isValid = jobject.IsValid(jschema, out validationEvents);
                    if (!isValid)
                    {
                        _logger.LogError("Error - JsonSchema Parse {MessageId} {Details}", id, validationEvents);
                        UpdateServiceBusMessageStatus(id, "F", $"Exception: {String.Join(",", validationEvents)}");
                    }
                }
            }
            catch (System.Exception exception)
            {
                _logger.LogError("Error - JsonSchema Parse {MessageId} {Details}", id, exception);
                UpdateServiceBusMessageStatus(id, "F", $"Exception: {exception.Message ?? ""} /n {exception.StackTrace ?? ""}");
            }

            return isValid;
        }

        private List<ServiceBusMessage> GetMessages()
        {
            ServiceBusMessage serviceBusMessage = null;
            List<ServiceBusMessage> serviceBusMessageList = new List<ServiceBusMessage>();
            System.Data.DataSet ds = SqlHelper.ExecuteDataset(_connectionString, "TF_GET_ServiceBusMessages");
            if (ds != null && ds.Tables.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    serviceBusMessage = new ServiceBusMessage();
                    serviceBusMessage.Id = dr.Field<int>("Id");
                    serviceBusMessage.Topic = dr["Topic"].ToString();
                    serviceBusMessage.JsonSchema = dr["JsonSchema"].ToString();
                    serviceBusMessage.Message = dr["Message"].ToString();
                    serviceBusMessage.IsRead = dr.Field<bool?>("IsRead");
                    serviceBusMessage.StatusFlag = dr["StatusFlag"].ToString();
                    serviceBusMessage.SentDate = dr.Field<DateTime?>("SentDate");
                    serviceBusMessage.PrimaryKey = dr["PrimaryKey"].ToString();
                    serviceBusMessage.PrimaryConnectionString = dr["PrimaryConnectionString"].ToString();
                    serviceBusMessage.SecondaryKey = dr["SecondaryKey"].ToString();
                    serviceBusMessage.SecondaryConnectionString = dr["SecondaryConnectionString"].ToString();
                    serviceBusMessage.IsEnabled = dr.Field<bool?>("IsEnabled");
                    serviceBusMessage.CreatedBy = dr["CreatedBy"].ToString();
                    serviceBusMessage.CreatedDate = dr.Field<DateTime?>("CreatedDate");
                    if (string.IsNullOrEmpty(serviceBusMessage.JsonSchema) || ValidateMessage(serviceBusMessage.Id,
                    serviceBusMessage.Message,
                    serviceBusMessage.JsonSchema))
                    {
                        serviceBusMessageList.Add(serviceBusMessage);
                    }
                }
            }
            return serviceBusMessageList;
        }

        private RetryExponential LoadRetryPolicy()
        {
            int.TryParse(_config.GetSection("RetryPolicy:MinimumBackoff").Value, out int minimumBackoff);
            int.TryParse(_config.GetSection("RetryPolicy:MaximumBackoff").Value, out int maximumBackoff);
            int.TryParse(_config.GetSection("RetryPolicy:MaximumRetryCount").Value, out int maximumRetryCount);

            return new RetryExponential(
               minimumBackoff: TimeSpan.FromSeconds(minimumBackoff == 0 ? 11 : minimumBackoff),
               maximumBackoff: TimeSpan.FromSeconds(maximumBackoff == 0 ? 31 : maximumBackoff),
               maximumRetryCount: maximumRetryCount == 0 ? 6 : maximumRetryCount);
        }

        private async Task SendMessagesAsync(List<ServiceBusMessage> list)
        {
            RetryExponential policy = LoadRetryPolicy();
            int id = 0;
            string statusFlag = string.Empty;
            try
            {
                var topics = list.Select(l => (l.Topic, l.PrimaryConnectionString)).Distinct().ToList();
                foreach (var topic in topics)
                {
                    try
                    {
                        topicClient = new TopicClient(topic.PrimaryConnectionString, topic.Topic, policy);
                        var topicList = list.Where(l => l.Topic == topic.Topic).ToList();
                        for (var i = 0; i < topicList.Count; i++)
                        {
                            try
                            {
                                // var @object = new { IHSSiteId = "IHS_ABC_001B", OperatorSiteId = "ABC001B" };
                                // var payload = JsonConvert.SerializeObject(@object);
                                id = topicList[i].Id;
                                var body = Encoding.UTF8.GetBytes(topicList[i].Message);
                                var message = new Message(body);

                                // Write the body of the message to the console
                                // Console.WriteLine($"Sending message: {i}");

                                // Send the message to the topic
                                await topicClient.SendAsync(message);
                                UpdateServiceBusMessageStatus(id, "S", null);
                                // Send the message to the queue
                                // await queueClient.SendAsync(message);
                            }
                            catch (Exception exception)
                            {
                                UpdateServiceBusMessageStatus(id, "F", $"Exception: {exception.Message ?? ""} /n {exception.StackTrace ?? ""}");
                                _logger.LogError("Error - Topic - Send Message {Details}", exception);
                                continue;
                            }
                        }
                        await topicClient.CloseAsync();
                    }
                    catch (Exception exception)
                    {
                        UpdateServiceBusMessageStatus(id, "F", $"Exception: {exception.Message ?? ""} /n {exception.StackTrace ?? ""}");
                        _logger.LogError("Error - Topic Connection {Details}", exception);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error - Topic {Details}", ex);
                throw ex;
            }
            finally
            {
                await topicClient.CloseAsync();
            }
        }

        public void UpdateServiceBusMessageStatus(int id, string statusFlag, string message)
        {
            Convert.ToInt32(SqlHelper.ExecuteScalar(_connectionString, "TF_Update_ServiceBusMessageStatus", id, statusFlag, message));
        }

        public async Task Publish()
        {
            List<ServiceBusMessage> list;
            try
            {
                list = GetMessages();
                if (list != null && list.Count > 0)
                {
                    // Send messages.
                    await SendMessagesAsync(list);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error - Reading Messages from DB {Details} ", ex);
            }
        }
    }
}