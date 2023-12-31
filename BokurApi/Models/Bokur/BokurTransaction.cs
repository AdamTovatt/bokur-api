﻿using RobinTTY.NordigenApiClient.Models.Responses;

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
            int? sibling)
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
    }
}
