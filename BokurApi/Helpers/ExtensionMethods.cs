using BokurApi.Models.Bokur;
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

            foreach(BokurTransaction transaction in transactions.Where(x => keyWord == null || x.Name.Contains(keyWord)))
            {
                if(transactions.Where(x => Math.Abs(x.Value) == Math.Abs(transaction.Value) && x.Date == transaction.Date).Count() == 2)
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
    }
}
