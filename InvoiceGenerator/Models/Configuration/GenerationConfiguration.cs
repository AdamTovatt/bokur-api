using System.Text.Json;
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

        private GenerationConfiguration()
        {
            InvoiceInformation = null!;
            GeneralInformation = null!;
        }

        public static GenerationConfiguration FromJson(string json)
        {
            GenerationConfiguration? result = JsonSerializer.Deserialize<GenerationConfiguration>(json);

            if (result == null)
                throw new JsonException($"Failed to deserialize json to GenerationConfiguration: {json}");

            return result;
        }
    }
}
