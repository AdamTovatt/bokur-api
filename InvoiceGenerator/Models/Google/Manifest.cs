using System.Text.Json.Serialization;

namespace InvoiceGenerator.Models.Google
{
    public class Manifest
    {
        [JsonPropertyName("fileRefs")]
        public List<FileRef>? FilesRefs { get; set; }
    }
}
