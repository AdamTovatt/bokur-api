using RobinTTY.NordigenApiClient.Models.Responses;
using System.Text.Json.Serialization;

namespace BokurApi.Models.Bokur
{
    public class BokurTransaction
    {
        public int Id { get; set; }
        public string? ExternalId { get; set; }
        public string Name { get; set; }
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
        public string? AssociatedFileName { get; set; }
        public BokurAccount? AffectedAccount { get; set; }
        public bool Ignored { get; set; }
        public bool IgnoreFileRequirement { get; set; }
        public int? ParentId { get; set; }
        public bool HasChildren { get; set; }
        public List<BokurTransaction>? Children { get; set; }
        public int? SiblingId { get; set; }

        public bool RequiresAction
        {
            get
            {
                return ExternalId != null && !Ignored && (AffectedAccount == null || AffectedAccount == null);
            }
        }

        [JsonConstructor]
        public BokurTransaction(
            int id,
            string? externalId,
            string name,
            decimal value,
            DateTime date,
            string? associatedFileName,
            BokurAccount? affectedAccount,
            bool ignored,
            int? parentId,
            bool hasChildren,
            int? siblingId,
            bool ignoreFileRequirement,
            bool requiresAction = false)
        {
            Id = id;
            Name = name;
            Value = value;
            Date = date;
            AssociatedFileName = associatedFileName;
            AffectedAccount = affectedAccount;
            ExternalId = externalId;
            Ignored = ignored;
            ParentId = parentId;
            HasChildren = hasChildren;
            SiblingId = siblingId;
            IgnoreFileRequirement = ignoreFileRequirement;
        }

        public BokurTransaction(
            int id,
            string? externalId,
            string name,
            decimal value,
            DateTime date,
            string? associatedFileName,
            BokurAccount? affectedAccount,
            bool ignored,
            int? parent,
            bool hasChildren,
            int? sibling,
            bool ignoreFileRequirement)
        {
            Id = id;
            Name = name;
            Value = value;
            Date = date;
            AssociatedFileName = associatedFileName;
            AffectedAccount = affectedAccount;
            ExternalId = externalId;
            Ignored = ignored;
            ParentId = parent;
            HasChildren = hasChildren;
            SiblingId = sibling;
            IgnoreFileRequirement = ignoreFileRequirement;
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

            return result.OrderByDescending(x => x.Date).ToList();
        }

        public override string ToString()
        {
            return $"{Name}: {Value.ToString("0.00")}";
        }

        public BokurEmail GetNewTransactionEmail()
        {

        }
    }
}
