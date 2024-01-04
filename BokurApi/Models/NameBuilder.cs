using System.Text;

namespace BokurApi.Models
{
    public class NameBuilder
    {
        private List<string> parts;

        public NameBuilder()
        {
            parts = new List<string>();
        }

        public static NameBuilder Create(string? value = null)
        {
            NameBuilder result = new NameBuilder();

            if (value != null)
                result.Append(value);

            return result;
        }

        public NameBuilder Append(string? value)
        {
            if (value == null)
                return this;

            if (parts.Count == 0)
                parts.Add(value);
            else if (!parts.Contains(value))
                parts.Add(value);

            return this;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (string part in parts)
                stringBuilder.Append(part);

            return stringBuilder.ToString();
        }
    }
}
