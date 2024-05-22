using System.Text;
using System.Text.Json.Serialization;

namespace InvoiceGenerator.Models.Data
{
    public class PaymentInformation
    {
        [JsonPropertyName("bic")]
        public string? Bic { get; set; }
        [JsonPropertyName("iban")]
        public string? Iban { get; set; }
        [JsonPropertyName("bankgiroNumber")]
        public string? BankgiroNumber { get; set; }

        public string? FormattedIban
        {
            get
            {
                if (Iban == null)
                    return null;

                StringBuilder result = new StringBuilder();

                for (int i = 0; i < Iban.Length; i++)
                {
                    result.Append(Iban[i]);

                    if (i > 1)
                    {
                        if ((i + 1) % 12 == 0)
                            result.AppendLine();
                        else if ((i + 1) % 4 == 0)
                            result.Append(" ");
                    }
                }

                return result.ToString().Trim();
            }
        }
    }
}
