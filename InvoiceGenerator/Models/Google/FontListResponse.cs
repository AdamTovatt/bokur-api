using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvoiceGenerator.Models.Google
{
    public class FontListResponse
    {
        [JsonPropertyName("manifest")]
        public Manifest? Manifest { get; set; }

        public static FontListResponse? FromJson(string json)
        {
            return JsonSerializer.Deserialize<FontListResponse>(json);
        }
    }
}
