using System.Text.RegularExpressions;

namespace BokurApi.Helpers
{
    public static class ExtensionMethods
    {
        public static string RemoveMultipleSpaces(this string input)
        {
            return Regex.Replace(input, @"\s+", " ");
        }
    }
}
