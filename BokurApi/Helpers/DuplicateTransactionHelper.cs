using BokurApi.Models.Bokur;
using System.Collections.Generic;
using System.Linq;

namespace BokurApi.Helpers
{
    public static class DuplicateTransactionHelper
    {
        public static List<BokurTransaction> FindTransactionsToRemove(List<BokurTransaction> originalTransactionList)
        {
            List<DuplicateTransactionPair> duplicatePairs = DuplicateTransactionPair.CreateListOfDuplicates(originalTransactionList);
            List<BokurTransaction> transactionsToRemove = duplicatePairs.Select(pair => pair.GetTransactionToRemove()).ToList();
            return transactionsToRemove;
        }
    }
} 