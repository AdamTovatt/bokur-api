using InvoiceGenerator.Helpers;
using InvoiceGenerator.Models.Data;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvoiceGenerator.Models.Configuration
{
    public class GeneralInformation
    {
        [JsonPropertyName("logoUrl")]
        public string? LogoUrl { get; set; }

        [JsonPropertyName("sender")]
        public Company? Sender { get; set; }

        [JsonPropertyName("paymentInformation")]
        public PaymentInformation? PaymentInformation { get; set; }

        [JsonPropertyName("defaultUnitPrice")]
        public int DefaultUnitPrice { get; set; }

        [JsonPropertyName("unitPriceOverride")]
        public Dictionary<string, int>? UnitPriceOverride { get; set; }

        public static GeneralInformation? FromJson(string json)
        {
            return JsonSerializer.Deserialize<GeneralInformation>(json);
        }

        public string ToJson()
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            return JsonSerializer.Serialize(this, options: options);
        }

        public byte[] GetLogoBytes()
        {
            if (LogoUrl == null)
                return new byte[0];

            return ApiHelper.Instance.GetImageBytes(LogoUrl).Result;
        }

        public int GetUnitPrice(string name)
        {
            if (UnitPriceOverride == null)
                return DefaultUnitPrice;

            if (UnitPriceOverride.TryGetValue(name, out int price))
                return price;

            return DefaultUnitPrice;
        }
    }
}
