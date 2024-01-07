namespace BokurApi.Models.Bokur
{
    public class AccountSummary
    {
        public BokurAccount Account { get; set; }
        public decimal Balance { get; set; }

        public AccountSummary(BokurAccount account, decimal balance)
        {
            Account = account;
            Balance = balance;
        }

        public override string ToString()
        {
            return $"{Account.Name}: {Balance.ToString("0.00")}";
        }
    }
}
