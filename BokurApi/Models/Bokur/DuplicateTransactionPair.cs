using BokurApi.Helpers;

namespace BokurApi.Models.Bokur
{
    public class DuplicateTransactionPair
    {
        public BokurTransaction TransactionA { get; private set; }
        public BokurTransaction TransactionB { get; private set; }

        public DuplicateTransactionPair(BokurTransaction transactionA, BokurTransaction transactionB)
        {
            TransactionA = transactionA;
            TransactionB = transactionB;
        }

        public bool HasSameTransactions(DuplicateTransactionPair other)
        {
            if (TransactionA.Id == other.TransactionA.Id || TransactionA.Id == other.TransactionB.Id)
            {
                if (TransactionB.Id == other.TransactionB.Id || TransactionB.Id == other.TransactionA.Id)
                    return true;
            }

            return false;
        }

        public static List<DuplicateTransactionPair> CreateListOfDuplicates(List<BokurTransaction> transactionList)
        {
            List<DuplicateTransactionPair> duplicatePairs = new List<DuplicateTransactionPair>();

            foreach (BokurTransaction transaction in transactionList)
            {
                List<BokurTransaction> possibleDuplicates = transaction.FindPossibleDuplicates(transactionList);

                foreach (BokurTransaction otherTransaction in possibleDuplicates)
                {
                    DuplicateTransactionPair possibleNewPair = new DuplicateTransactionPair(transaction, otherTransaction);

                    if (duplicatePairs.Any(x => x.HasSameTransactions(possibleNewPair)))
                        continue;

                    duplicatePairs.Add(possibleNewPair);
                }
            }

            duplicatePairs = duplicatePairs.Where(x => x.IsActuallyValidPair()).ToList();
            duplicatePairs = GetCleanedDuplicatePairs(duplicatePairs);

            return duplicatePairs;
        }

        public bool IsActuallyValidPair()
        {
            if (TransactionA.Date != TransactionB.Date) return false;
            if (TransactionA.Value != TransactionB.Value) return false;

            double similarity = StringSimilarity.CosineSimilarity(TransactionA.Name, TransactionB.Name, ignoreDuplicateWords: true);
            return similarity >= 0.9;
        }

        public override string ToString()
        {
            return $"{TransactionA.Name} / {TransactionB.Name}";
        }

        // sometimes, if there are double duplicate pairs, we get four duplicates, remove them
        private static List<DuplicateTransactionPair> GetCleanedDuplicatePairs(List<DuplicateTransactionPair> pairs)
        {
            HashSet<int> containedIds = new HashSet<int>();

            foreach (DuplicateTransactionPair pair in pairs)
            {
                if (!containedIds.Contains(pair.TransactionA.Id) && !containedIds.Contains(pair.TransactionB.Id))
                {
                    containedIds.Add(pair.TransactionA.Id);
                }
            }

            List<DuplicateTransactionPair> cleanedPairs = new List<DuplicateTransactionPair>();

            HashSet<int> addedIds = new HashSet<int>();

            foreach (DuplicateTransactionPair pair in pairs)
            {
                if (containedIds.Contains(pair.TransactionA.Id) && !addedIds.Contains(pair.TransactionA.Id) && !addedIds.Contains(pair.TransactionB.Id))
                {
                    addedIds.Add(pair.TransactionA.Id);
                    addedIds.Add(pair.TransactionB.Id);
                    cleanedPairs.Add(pair);
                }
            }

            return cleanedPairs;
        }

        public BokurTransaction GetTransactionToRemove()
        {
            if (TransactionA.Name.Length >= TransactionB.Name.Length) return TransactionA;
            return TransactionB;
        }
    }
}
