using System;
namespace IHS.ASB.Core.Models
{
    public class ServiceBusMessage
    {
        public int Id { get; set; }
        public string Topic { get; set; }
        public string JsonSchema { get; set; }
        public string Message { get; set; }
        public bool? IsRead { get; set; }
        public string StatusFlag { get; set; }
        public string PrimaryKey { get; set; }
        public string PrimaryConnectionString { get; set; }
        public string SecondaryKey { get; set; }
        public string SecondaryConnectionString { get; set; }
        public bool? IsEnabled { get; set; }

        public DateTime? SentDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}