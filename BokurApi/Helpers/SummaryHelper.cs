using BokurApi.Models.Bokur;
using BokurApi.Repositories.Transaction;
using RobinTTY.NordigenApiClient.Models.Responses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BokurApi.Helpers
{
    public class SummaryHelper
    {
        private readonly ITransactionRepository _transactionRepository;

        public SummaryHelper(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<MonthlySummary> CreateMonthlySummaryAsync(DateTime startDate, DateTime endDate)
        {
            List<DateTime> months = GetFirstDaysOfMonth(startDate, endDate);

            List<SummaryPerMonth> result = new List<SummaryPerMonth>();

            foreach (DateTime month in months)
            {
                List<BokurTransaction> transactions = await _transactionRepository.GetAllForMonthAsync(month);

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

        private List<DateTime> GetFirstDaysOfMonth(DateTime startDate, DateTime endDate)
        {
            List<DateTime> months = new List<DateTime>();
            DateTime current = new DateTime(startDate.Year, startDate.Month, 1);
            DateTime end = new DateTime(endDate.Year, endDate.Month, 1);

            while (current <= end)
            {
                months.Add(current);
                current = current.AddMonths(1);
            }

            return months;
        }
    }
}
