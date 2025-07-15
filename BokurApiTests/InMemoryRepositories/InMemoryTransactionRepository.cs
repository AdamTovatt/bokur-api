using BokurApi.Models.Bokur;
using BokurApi.Repositories.Account;
using BokurApi.Repositories.Transaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BokurApiTests.InMemoryRepositories
{
    public class InMemoryTransactionRepository : ITransactionRepository
    {
        private readonly List<BokurTransaction> transactions = new List<BokurTransaction>();
        private int nextId = 1;

        private BokurTransaction CloneTransaction(BokurTransaction transaction)
        {
            return new BokurTransaction(
                transaction.Id,
                transaction.ExternalId,
                transaction.Name,
                transaction.Value,
                transaction.Date,
                transaction.AssociatedFileName,
                transaction.AffectedAccount == null ? null : new BokurAccount(
                    transaction.AffectedAccount.Id,
                    transaction.AffectedAccount.Name,
                    transaction.AffectedAccount.Email
                ),
                transaction.Ignored,
                transaction.ParentId,
                transaction.HasChildren,
                transaction.SiblingId,
                transaction.IgnoreFileRequirement
            );
        }

        public Task<int> CreateAsync(BokurTransaction transaction)
        {
            BokurTransaction copy = CloneTransaction(transaction);
            copy.Id = nextId;
            nextId++;
            transactions.Add(copy);
            return Task.FromResult(copy.Id);
        }

        public Task UpdateAsync(BokurTransaction transaction)
        {
            int index = transactions.FindIndex(existingTransaction => existingTransaction.Id == transaction.Id);
            if (index >= 0)
            {
                BokurTransaction existing = transactions[index];
                existing.Name = transaction.Name;
                existing.Value = transaction.Value;
                existing.Date = transaction.Date;
                existing.AssociatedFileName = transaction.AssociatedFileName;
                existing.AffectedAccount = transaction.AffectedAccount;
                existing.Ignored = transaction.Ignored;
                existing.IgnoreFileRequirement = transaction.IgnoreFileRequirement;
                existing.ParentId = transaction.ParentId;
                existing.HasChildren = transaction.HasChildren;
                existing.SiblingId = transaction.SiblingId;
            }
            return Task.CompletedTask;
        }

        public Task RemoveAssociatedFileAsync(int transactionId)
        {
            foreach (BokurTransaction transaction in transactions)
            {
                if (transaction.Id == transactionId)
                {
                    transaction.AssociatedFileName = null;
                    break;
                }
            }
            return Task.CompletedTask;
        }

        public Task SetAffectedAccountAsync(int transactionId, int accountId)
        {
            foreach (BokurTransaction transaction in transactions)
            {
                if (transaction.Id == transactionId && transaction.AffectedAccount != null)
                {
                    transaction.AffectedAccount.Id = accountId;
                    break;
                }
            }
            return Task.CompletedTask;
        }

        public Task<bool> CreateTransferAsync(int parentTransactionId, int toAccountId, decimal amount)
        {
            // Not needed for basic controller tests, can be implemented if required
            return Task.FromResult(true);
        }

        public Task SetTransactionValueAsync(int transactionId, decimal newValue)
        {
            foreach (BokurTransaction transaction in transactions)
            {
                if (transaction.Id == transactionId)
                {
                    transaction.Value = newValue;
                    break;
                }
            }
            return Task.CompletedTask;
        }

        public Task DeleteTransactionAsync(int transactionId)
        {
            transactions.RemoveAll(transaction => transaction.Id == transactionId);
            return Task.CompletedTask;
        }

        public Task<BokurTransaction?> GetByIdAsync(int id)
        {
            foreach (BokurTransaction transaction in transactions)
            {
                if (transaction.Id == id)
                    return Task.FromResult<BokurTransaction?>(CloneTransaction(transaction));
            }
            return Task.FromResult<BokurTransaction?>(null);
        }

        public Task<List<BokurTransaction>> GetAllThatRequiresActionAsync()
        {
            return Task.FromResult(new List<BokurTransaction>(transactions));
        }

        public Task<List<BokurTransaction>> GetAllChildrenForParentAsync(Npgsql.NpgsqlConnection connection, int parentId)
        {
            // Not needed for basic controller tests
            return Task.FromResult(new List<BokurTransaction>());
        }

        public Task<List<BokurTransaction>> GetAllForMonthAsync(DateTime month)
        {
            List<BokurTransaction> result = new List<BokurTransaction>();
            foreach (BokurTransaction transaction in transactions)
            {
                if (transaction.Date.Month == month.Month && transaction.Date.Year == month.Year)
                    result.Add(transaction);
            }
            return Task.FromResult(result);
        }

        public Task<List<BokurTransaction>> GetTransactionsForExport(DateTime startDate, DateTime endDate)
        {
            List<BokurTransaction> result = new List<BokurTransaction>();
            foreach (BokurTransaction transaction in transactions)
            {
                if (transaction.Date >= startDate && transaction.Date <= endDate)
                    result.Add(transaction);
            }
            return Task.FromResult(result);
        }

        public Task<List<BokurTransaction>> GetAllWithoutParentAsync(int pageSize = 10, int page = 0, int? accountId = null)
        {
            List<BokurTransaction> result = new List<BokurTransaction>();
            int skip = page * pageSize;
            int take = pageSize;
            
            List<BokurTransaction> filteredTransactions = transactions;
            if (accountId.HasValue)
            {
                filteredTransactions = transactions.Where(t => t.AffectedAccount?.Id == accountId.Value).ToList();
            }
            
            for (int i = skip; i < skip + take && i < filteredTransactions.Count; i++)
            {
                result.Add(filteredTransactions[i]);
            }
            return Task.FromResult(result);
        }

        public Task<List<string>> GetExistingExternalIdsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            List<string> externalIds = new List<string>();
            foreach (BokurTransaction transaction in transactions)
            {
                if (transaction.ExternalId != null)
                    externalIds.Add(transaction.ExternalId);
            }
            return Task.FromResult(externalIds);
        }

        public Task<List<AccountSummary>> GetSummaryAsync()
        {
            // Not needed for basic controller tests
            return Task.FromResult(new List<AccountSummary>());
        }
    }

    public class InMemoryAccountRepository : IAccountRepository
    {
        private readonly List<BokurAccount> accounts = new List<BokurAccount>();
        private int nextId = 1;

        public Task CreateAsync(string name)
        {
            BokurAccount account = new BokurAccount(nextId, name, null);
            accounts.Add(account);
            nextId++;
            return Task.CompletedTask;
        }

        public Task SetName(int id, string name)
        {
            BokurAccount? account = accounts.FirstOrDefault(a => a.Id == id);
            if (account != null)
            {
                account.Name = name;
            }
            return Task.CompletedTask;
        }

        public Task<List<BokurAccount>> GetAllAsync()
        {
            return Task.FromResult(new List<BokurAccount>(accounts));
        }

        public Task<BokurAccount?> GetByIdAsync(int id)
        {
            BokurAccount? account = accounts.FirstOrDefault(a => a.Id == id);
            return Task.FromResult(account);
        }
    }
}