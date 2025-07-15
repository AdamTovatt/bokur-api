using BokurApi.Models.Bokur;
using RobinTTY.NordigenApiClient.Models.Responses;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BokurApi.Helpers
{
    public static class ExtensionMethods
    {
        public static string RemoveMultipleSpaces(this string input)
        {
            return Regex.Replace(input, @"\s+", " ");
        }

        public static List<BokurTransaction> RemoveInternalTransactions(this List<BokurTransaction> transactions, string? keyWord = null)
        {
            List<BokurTransaction> transactionsToRemove = new List<BokurTransaction>();

            foreach (BokurTransaction transaction in transactions.Where(x => keyWord == null || x.Name.Contains(keyWord)))
            {
                if (transactions.Where(x => Math.Abs(x.Value) == Math.Abs(transaction.Value) && x.Date == transaction.Date).Count() == 2)
                {
                    transactionsToRemove.Add(transaction);
                }
            }

            if (transactionsToRemove.Count == 0)
                return transactions;

            return transactions.Where(x => !transactionsToRemove.Contains(x)).ToList();
        }

        public static DateOnly? ToDateOnly(this DateTime? nullableDateTime)
        {
            if (nullableDateTime == null) return null;
            DateTime dateTime = (DateTime)nullableDateTime;
            return new DateOnly(dateTime.Year, dateTime.Month, dateTime.Day);
        }

        public static DateOnly ToDateOnly(this DateTime dateTime)
        {
            return new DateOnly(dateTime.Year, dateTime.Month, dateTime.Day);
        }

        public static string ApplyParameters(this string text, params object[]? parameters)
        {
            if (parameters == null || !text.Contains('{'))
                return text;

            string result = text;

            foreach (object parameter in parameters)
            {
                foreach (PropertyInfo propertyInfo in parameter.GetType().GetProperties())
                {
                    string propertyName = propertyInfo.Name;
                    object? propertyValue = propertyInfo.GetValue(parameter);

                    if (propertyValue == null)
                        continue;

                    result = result.Replace($"{{{{{propertyName}}}}}", propertyValue.ToString());
                }
            }

            return result;
        }

        public static int GetDaysLeft(this Requisition requisition)
        {
            return 90 - (int)Math.Ceiling((DateTime.Now - requisition.Created).TotalDays);
        }

        public static List<string> RemoveNullValues(this IEnumerable<string?> originalValues)
        {
            return originalValues.Where(x => x != null).Select(x => x!).ToList();
        }
    }
}
