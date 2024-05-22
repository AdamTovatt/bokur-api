using System.Text.Json.Serialization;

namespace InvoiceGenerator.Models.Data
{
    public class Address
    {
        [JsonPropertyName("firstLine")]
        public string? FirstLine { get; set; }
        [JsonPropertyName("secondLine")]
        public string? SecondLine { get; set; }
    }
}
