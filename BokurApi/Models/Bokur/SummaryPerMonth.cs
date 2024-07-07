namespace BokurApi.Models.Bokur
{
    public class SummaryPerMonth
    {
        public DateTime FirstDayOfMonth { get; set; }
        public string MonthName { get; set; }
        public int Year { get; set; }

        public List<AccountWithTransactions> AffectedAccounts { get; set; }

        public SummaryPerMonth(DateTime firstDayOfMonth, string monthName, int year)
        {
            FirstDayOfMonth = firstDayOfMonth;
            MonthName = monthName;
            Year = year;
            AffectedAccounts = new List<AccountWithTransactions>();
        }

        public List<SimplifiedSummaryForAccountPerMonth> GetSimplifiedSummary()
        {
            List<SimplifiedSummaryForAccountPerMonth> simplifiedSummary = new List<SimplifiedSummaryForAccountPerMonth>();

            foreach (AccountWithTransactions accountWithTransactions in AffectedAccounts)
            {
                SimplifiedSummaryForAccountPerMonth simplifiedSummaryForAccountPerMonth = new SimplifiedSummaryForAccountPerMonth(
                    MonthName,
                    accountWithTransactions.Account.Name,
                    accountWithTransactions.TotalIn,
                    accountWithTransactions.TotalOut,
                    accountWithTransactions.TotalChange);

                simplifiedSummary.Add(simplifiedSummaryForAccountPerMonth);
            }

            return simplifiedSummary;
        }

        public override string ToString()
        {
            return MonthName;
        }
    }

    public class SimplifiedSummaryForAccountPerMonth
    {
        public string MonthName { get; set; }
        public string AccountName { get; set; }
        public decimal TotalIn { get; set; }
        public decimal TotalOut { get; set; }
        public decimal TotalChange { get; set; }

        public SimplifiedSummaryForAccountPerMonth(string monthName, string accountName, decimal totalIn, decimal totalOut, decimal totalChange)
        {
            MonthName = monthName;
            AccountName = accountName;
            TotalIn = totalIn;
            TotalOut = totalOut;
            TotalChange = totalChange;
        }
    }

    public class AccountWithTransactions
    {
        public BokurAccount Account { get; set; }
        public List<BokurTransaction> Transactions { get; set; }

        public decimal TotalIn { get; set; }
        public decimal TotalOut { get; set; }
        public decimal TotalChange { get; set; }

        public AccountWithTransactions(BokurAccount account, List<BokurTransaction> transactions)
        {
            Account = account;
            Transactions = transactions;

            foreach (BokurTransaction transaction in Transactions)
            {
                if (transaction.Value > 0)
                    TotalIn += transaction.Value;
                else
                    TotalOut -= transaction.Value;
            }

            TotalChange = TotalIn - TotalOut;
        }

        public override string ToString()
        {
            return Account.Name;
        }
    }
}
