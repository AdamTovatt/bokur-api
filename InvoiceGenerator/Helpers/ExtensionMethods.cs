using System.Globalization;
using System.Text;

namespace InvoiceGenerator.Helpers
{
    public static class ExtensionMethods
    {
        public static string GetUnescapedValue(this string value)
        {
            if (value[value.Length - 1] == '"')
                return value.Substring(1, value.Length - 2);
            return value.Substring(1, value.Length - 1);
        }

        public static string ToDateString(this DateTime date)
        {
            return date.ToString("dd-MM-yyyy");
        }

        public static string ToPriceString(this int price, string? currency = null)
        {
            string reference = price.ToString();

            StringBuilder result = new StringBuilder();

            int stepDistance = 0;

            for (int i = reference.Length - 1; i >= 0; i--)
            {
                result.Insert(0, reference[i]);

                if (stepDistance > 1)
                {
                    if ((stepDistance + 1) % 3 == 0)
                        result.Insert(0, " ");
                }

                stepDistance++;
            }

            if (currency != null)
                result.Append($" {currency}");

            return result.ToString();
        }

        public static string ToPriceString(this double price, string? currency = null)
        {
            string toString = price.ToString("0.00", CultureInfo.InvariantCulture);
            string reference = toString.Split('.')[0];

            StringBuilder result = new StringBuilder();

            int stepDistance = 0;

            for (int i = reference.Length - 1; i >= 0; i--)
            {
                result.Insert(0, reference[i]);

                if (stepDistance > 1)
                {
                    if ((stepDistance + 1) % 3 == 0)
                        result.Insert(0, " ");
                }

                stepDistance++;
            }

            string[] parts = toString.Split('.');

            if (parts.Length == 2)
                result.Append($".{parts[1]}");

            if (currency != null)
                result.Append($" {currency}");

            return result.ToString();
        }
    }
}
