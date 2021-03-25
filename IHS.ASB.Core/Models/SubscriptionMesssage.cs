using System;

namespace IHS.ASB.Core.Models
{
    public class SubscriptionMesssage
    {
        public int Id { get; set; }
        public string TopicName { get; set; }
        public string JsonSchema { get; set; }
        public string SubscriptionName { get; set; }
        public string Description { get; set; }
        public string TypeOfTopic { get; set; }
        public string PrimaryKey { get; set; }
        public string PrimaryConnectionString { get; set; }
        public string SecondaryKey { get; set; }
        public string SecondaryConnectionString { get; set; }
        public bool? IsEnabled { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}