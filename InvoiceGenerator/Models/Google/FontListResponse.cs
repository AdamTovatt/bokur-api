using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
