using RobinTTY.NordigenApiClient.Models.Responses;

namespace BokurApi.Models.Bokur
{
    public class BokurTransaction
    {
        public int Id { get; set; }
        public string ExternalId { get; set; }
        public string Name { get; set; }
        public decimal Value { get; set; }
        public DateTime TimeOfCreation { get; set; }
        public BokurFile? AssociatedFile { get; set; }
        public BokurAccount? TargetAccount { get; set; }

        public BokurTransaction(int id, string name, decimal value, DateTime timeOfCreation, BokurFile? associatedFile, BokurAccount? targetAccount, string externalId)
        {
            Id = id;
            Name = name;
            Value = value;
            TimeOfCreation = timeOfCreation;
            AssociatedFile = associatedFile;
            TargetAccount = targetAccount;
            ExternalId = externalId;
        }

        public BokurTransaction(Transaction transaction)
        {

            NameBuilder name = NameBuilder.Create()
                .Append(transaction.RemittanceInformationUnstructured)
                .Append(transaction.DebtorName)
                .Append(transaction.CreditorName);

            Name = name.ToString();
            Value = transaction.TransactionAmount.Amount;
            ExternalId = transaction.InternalTransactionId ?? "";
            TimeOfCreation = transaction.BookingDate ?? DateTime.MinValue;
        }

        public static List<BokurTransaction> GetList(List<Transaction> transactions)
        {
            List<BokurTransaction> result = new List<BokurTransaction>();

            foreach (Transaction transaction in transactions)
                result.Add(new BokurTransaction(transaction));

            return result;
        }
    }
}
