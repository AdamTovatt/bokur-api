using InvoiceGenerator.Models.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvoiceGenerator.Models.Configuration
{
    public class InvoiceInformation
    {
        [JsonPropertyName("number")]
        public string? Number { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("daysToPay")]
        public int DaysToPay { get; set; }

        [JsonPropertyName("includeTax")]
        public bool IncludeTax { get; set; }

        [JsonPropertyName("swedish")]
        public bool Swedish { get; set; }

        [JsonPropertyName("receiver")]
        public Company? Receiver { get; set; }

        [JsonPropertyName("issueDate")]
        public DateTime IssueDate { get; set; } = DateTime.Now;

        [JsonPropertyName("includeBreakdown")]
        public bool IncludeBreakdown { get; set; } = true;

        //method for serializing the object to json
        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        //method for deserializing the object from json
        public static InvoiceInformation? FromJson(string? json)
        {
            if (json == null)
                return null;

            return JsonSerializer.Deserialize<InvoiceInformation>(json);
        }
    }
}
