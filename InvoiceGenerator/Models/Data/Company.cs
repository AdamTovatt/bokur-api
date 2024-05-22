using System.Text.Json.Serialization;

namespace InvoiceGenerator.Models.Data
{
    public class Company
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("address")]
        public Address? Address { get; set; }
        [JsonPropertyName("organizationNumber")]
        public string? OrganizationNumber { get; set; }
        [JsonPropertyName("reference")]
        public Person? Reference { get; set; }
    }
}
