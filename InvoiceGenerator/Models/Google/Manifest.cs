using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace InvoiceGenerator.Models.Google
{
    public class Manifest
    {
        [JsonPropertyName("fileRefs")]
        public List<FileRef>? FilesRefs { get; set; }
    }
}
