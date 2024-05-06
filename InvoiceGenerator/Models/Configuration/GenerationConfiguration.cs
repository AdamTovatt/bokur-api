using System.Text.Json.Serialization;

namespace InvoiceGenerator.Models.Configuration
{
    public class GenerationConfiguration
    {
        [JsonPropertyName("invoiceInformation")]
        public InvoiceInformation InvoiceInformation { get; set; }

        [JsonPropertyName("generalInformation")]
        public GeneralInformation GeneralInformation { get; set; }

        [JsonConstructor]
        public GenerationConfiguration(InvoiceInformation invoiceInformation, GeneralInformation generalInformation)
        {
            InvoiceInformation = invoiceInformation;
            GeneralInformation = generalInformation;
        }
    }
}
