using InvoiceGenerator.Models;

namespace InvoiceGenerator.Helpers
{
    public static class LanguageConverter
    {
        private static Dictionary<string, string> englishToSwedishDictionary = new Dictionary<string, string>()
        {
            { "Sender", "Avsändare" },
            { "Recipient", "Mottagare" },
            { "Invoice number", "Fakturanummer" },
            { "Invoice date", "Fakturadatum" },
            { "Invoice due date", "Förfallodatum" },
            { "INVOICE", "FAKTURA" },
            { "Sender reference", "Avsändarreferens" },
            { "Recipient reference", "Mottagarreferens" },
            { "Payment Information", "Betalningsinformation" },
            { "Description", "Beskrivning" },
            { "Quantity", "Mängd" },
            { "Unit", "Enhet" },
            { "Unit price", "á pris" },
            { "Total", "Totalt" },
            { "Total before VAT", "Totalt belopp före moms" },
            { "Total VAT", "Total moms" },
            { "Amount to pay", "Summa att betala" },
            { "Contact information", "Kontaktinformation" },
            { "Organization nr:", "Organisationsnr:" },
            { "hours", "timmar" },
            { "SEK", "kr" },
            { "Name:", "Nam:" },
            { "Phone:", "Telefon:" },
            { "Email:", "Mail:" },
            { "VAT%", "Moms %" },
            { "VAT", "Moms" },
            { "Comment:", "Kommentar:" },
            { "Hours", "Timmar" },
        };

        public static string ToCorrectLanguage(this string text, Invoice invoice)
        {
            if (!invoice.GeneralInformation.Swedish)
                return text;

            if (englishToSwedishDictionary.TryGetValue(text, out string? swedish))
                return swedish;

            return $"[missing translation for {text}";
        }
    }
}
