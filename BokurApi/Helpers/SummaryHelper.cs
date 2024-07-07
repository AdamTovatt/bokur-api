using BokurApi.Models.Bokur;
using BokurApi.Repositories;
using RobinTTY.NordigenApiClient.Models.Responses;

namespace BokurApi.Helpers
{
    public class SummaryHelper
    {
        public static async Task<MonthlySummary> CreateMonthlySummaryAsync(DateTime startDate, DateTime endDate)
        {
            List<DateTime> months = GetFirstDaysOfMonth(startDate, endDate);

            List<SummaryPerMonth> result = new List<SummaryPerMonth>();

            foreach (DateTime month in months)
            {
                List<BokurTransaction> transactions = await TransactionRepository.Instance.GetAllForMonthAsync(month);

                if (transactions.Count == 0)
                    continue;

                SummaryPerMonth summary = new SummaryPerMonth(month, month.ToString("MMMM"), month.Year);

                Dictionary<BokurAccount, List<BokurTransaction>> accountTransactions = new Dictionary<BokurAccount, List<BokurTransaction>>();

                foreach (BokurTransaction transaction in transactions)
                {
                    if (transaction.AffectedAccount == null) continue;

                    if (!accountTransactions.ContainsKey(transaction.AffectedAccount))
                        accountTransactions.Add(transaction.AffectedAccount, new List<BokurTransaction>());

                    accountTransactions[transaction.AffectedAccount].Add(transaction);
                }

                foreach (KeyValuePair<BokurAccount, List<BokurTransaction>> accountTransaction in accountTransactions)
                {
                    summary.AffectedAccounts.Add(new AccountWithTransactions(accountTransaction.Key, accountTransaction.Value));
                }

                result.Add(summary);
            }

            return new MonthlySummary(result);
        }

        private static List<DateTime> GetFirstDaysOfMonth(DateTime start, DateTime end)
        {
            List<DateTime> firstDays = new List<DateTime>();

            DateTime current = new DateTime(start.Year, start.Month, 1);
            DateTime endOfEndMonth = new DateTime(end.Year, end.Month, 1);

            while (current <= endOfEndMonth)
            {
                firstDays.Add(current);
                current = current.AddMonths(1);
            }

            return firstDays;
        }
    }
}
