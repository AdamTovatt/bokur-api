using BokurApi.Models.Bokur;

namespace BokurApiTests.TestUtilities
{
    public static class ExtensionMethods
    {
        public static BokurTransaction TakeByAccount(this List<BokurTransaction> list, string name)
        {
            BokurTransaction? transaction = list.Where(x => x.Name == name).FirstOrDefault();

            if(transaction == null)
                throw new Exception($"No transaction with name {name} found");

            return transaction;
        }

        public static int TakeAccountId(this List<AccountSummary> list, string name)
        {
            AccountSummary? account = list.Where(x => x.Account.Name == name).FirstOrDefault();

            if(account == null)
                throw new Exception($"No account with name {name} found");

            return account.Account.Id;
        }

        public static AccountSummary TakeByAccount(this List<AccountSummary> list, string name)
        {
            AccountSummary? account = list.Where(x => x.Account.Name == name).FirstOrDefault();

            if(account == null)
                throw new Exception($"No account with name {name} found");

            return account;
        }
    }
}
