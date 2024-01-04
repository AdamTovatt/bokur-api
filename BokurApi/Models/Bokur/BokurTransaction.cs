using RobinTTY.NordigenApiClient.Models.Responses;

namespace BokurApi.Models.Bokur
{
    public class BokurTransaction
    {
        public int Id { get; set; }
        public string ExternalId { get; set; }
        public string Name { get; set; }
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
        public string? AssociatedFileName { get; set; }
        public BokurAccount? AffectedAccount { get; set; }
        public bool Ignored { get; set; }

        public BokurTransaction(int id, string name, decimal value, DateTime timeOfCreation, string? associatedFileName, BokurAccount? affectedAccount, string externalId, bool ignored)
        {
            Id = id;
            Name = name;
            Value = value;
            Date = timeOfCreation;
            AssociatedFileName = associatedFileName;
            AffectedAccount = affectedAccount;
            ExternalId = externalId;
            Ignored = ignored;
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
            Date = transaction.BookingDate ?? DateTime.Now;
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
