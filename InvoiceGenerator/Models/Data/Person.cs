using System.Text.Json.Serialization;

namespace InvoiceGenerator.Models.Data
{
    public class Person
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }
    }
}
