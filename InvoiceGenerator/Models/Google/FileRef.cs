using System.Text.Json.Serialization;

namespace InvoiceGenerator.Models.Google
{
    public class FileRef
    {
        [JsonPropertyName("filename")]
        public string? Name { get; set; }
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        public string VariationName
        {
            get
            {
                if (_variantName == null)
                    _variantName = GetVariantName();
                return _variantName;
            }
        }

        private string? _variantName;

        private string GetVariantName()
        {
            string variantName = (Name ?? string.Empty).ToLower();
            return variantName.Split('.')[0].Split('-').LastOrDefault() ?? string.Empty;
        }

        public override string ToString()
        {
            return Name ?? "(Missing font name)";
        }
    }
}
