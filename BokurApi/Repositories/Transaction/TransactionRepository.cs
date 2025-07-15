using BokurApi.Models.Bokur;
using BokurApi.Helpers;
using Dapper;
using Npgsql;
using BokurApi.Models.Exceptions;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;
using BokurApi.Repositories.Account;
using BokurApi.Helpers.DatabaseConnection;

namespace BokurApi.Repositories.Transaction
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IAccountRepository _accountRepository;

        public TransactionRepository(
            IDbConnectionFactory connectionFactory,
            IAccountRepository accountRepository
        )
        {
            _connectionFactory = connectionFactory;
            _accountRepository = accountRepository;
        }

        public async Task<int> CreateAsync(BokurTransaction transaction)
        {
            const string query = $@"INSERT INTO bokur_transaction
                                    (external_id, name, value, date, parent, affected_account) VALUES
                                    (
                                        @{nameof(transaction.ExternalId)},
                                        @{nameof(transaction.Name)},
                                        @{nameof(transaction.Value)},
                                        @{nameof(transaction.Date)},
                                        @{nameof(transaction.ParentId)},
                                        @{nameof(transaction.AffectedAccount.Id)}
                                    )
                                    RETURNING id";

            try
            {
                using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
                    return await connection.ExecuteScalarAsync<int>(query, new
                    {
                        transaction.ExternalId,
                        transaction.Name,
                        transaction.Value,
                        transaction.Date,
                        transaction.ParentId,
                        transaction.AffectedAccount?.Id,
                    });
            }
            catch (PostgresException exception)
            {
                if (exception.SqlState == "23505")
                    throw new ApiException($"Transaction with external id {transaction.ExternalId} already exists", HttpStatusCode.BadRequest);
                else
                    throw;
            }
        }

        public async Task UpdateAsync(BokurTransaction transaction)
        {
            if (transaction == null || transaction.Id == 0)
                throw new ApiException("Invalid transaction object or missing transaction id", HttpStatusCode.BadRequest);

            if (transaction.Name != null && transaction.Name.Length < 1)
                throw new ApiException("Name cannot be less than 1 character", HttpStatusCode.BadRequest);

            const string query = @$"
                UPDATE bokur_transaction
                SET
                    name = COALESCE(@{nameof(transaction.Name)}, name),
                    associated_file_name = COALESCE(@{nameof(transaction.AssociatedFileName)}, associated_file_name),
                    affected_account = COALESCE(@{nameof(transaction.AffectedAccount.Id)}, affected_account),
                    ignored = COALESCE(@{nameof(transaction.Ignored)}, ignored),
                    ignore_file_requirement = COALESCE(@{nameof(transaction.IgnoreFileRequirement)}, ignore_file_requirement)
                WHERE id = @transactionId";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
                await connection.ExecuteAsync(query, new
                {
                    transactionId = transaction.Id,
                    transaction.Name,
                    transaction.AssociatedFileName,
                    transaction.AffectedAccount?.Id,
                    transaction.Ignored,
                    transaction.IgnoreFileRequirement,
                });
        }

        public async Task RemoveAssociatedFileAsync(int transactionId)
        {
            const string query = $@"UPDATE bokur_transaction SET associated_file_name = NULL WHERE id = @{nameof(transactionId)}";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
                await connection.ExecuteAsync(query, new { transactionId });
        }

        public async Task SetAffectedAccountAsync(int transactionId, int accountId)
        {
            if (accountId <= 0)
                throw new ApiException("Invalid account id", HttpStatusCode.BadRequest);

            BokurAccount? account = await _accountRepository.GetByIdAsync(accountId);

            if (account == null)
                throw new ApiException($"No account with id {accountId} exists", HttpStatusCode.BadRequest);

            const string query = $@"UPDATE bokur_transaction SET affected_account = @{nameof(accountId)} WHERE id = @{nameof(transactionId)}";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
                await connection.ExecuteAsync(query, new { transactionId, accountId });
        }

        public async Task<bool> CreateTransferAsync(int parentTransactionId, int toAccountId, decimal amount)
        {
            BokurTransaction? parent = await GetByIdAsync(parentTransactionId);

            if (parent == null)
                throw new ApiException($"No transaction with id {parentTransactionId} exists", HttpStatusCode.BadRequest);

            if (parent.AffectedAccount == null)
                throw new ApiException($"Parent transaction with id {parentTransactionId} has no affected account, can't transfer money from it", HttpStatusCode.BadRequest);

            BokurAccount fromAccount = parent.AffectedAccount;
            BokurAccount? toAccount = await _accountRepository.GetByIdAsync(toAccountId);

            if (toAccount == null)
                throw new ApiException($"No account with id {toAccountId} exists", HttpStatusCode.BadRequest);

            if (fromAccount.Id == toAccount.Id)
                throw new ApiException("From and to account cannot be the same", HttpStatusCode.BadRequest);

            if (amount == 0)
                throw new ApiException("Amount cannot be zero", HttpStatusCode.BadRequest);

            BokurTransaction outTransaction = new BokurTransaction(0, null, $"Transfer from {fromAccount.Name}", amount * -1, parent.Date, null, fromAccount, false, parent.Id, false, null, false);
            BokurTransaction inTransaction = new BokurTransaction(0, null, $"Transfer to {toAccount.Name}", amount, parent.Date, null, toAccount, false, parent.Id, false, null, false);

            int outId = await CreateAsync(outTransaction);
            int inId = await CreateAsync(inTransaction);

            await SetSiblingAsync(outId, inId);
            await SetSiblingAsync(inId, outId);

            await SetHasChildrenAsync(parentTransactionId, true);

            return outId != 0 && inId != 0;
        }

        public async Task SetTransactionValueAsync(int transactionId, decimal newValue)
        {
            BokurTransaction? transaction = await GetByIdAsync(transactionId);

            if (transaction == null)
                throw new ApiException($"No transacstion with id {transactionId}, can't set value", HttpStatusCode.BadRequest);

            if (transaction.ExternalId != null)
                throw new ApiException($"Transaction with id {transactionId} is a transaction from an external source, can't set value", HttpStatusCode.BadRequest);

            if (transaction.SiblingId != null)
            {
                await SetSingleTransactionValueAsync((int)transaction.SiblingId, newValue * -1);
                await SetSingleTransactionValueAsync(transactionId, newValue);
            }
        }

        private async Task SetSingleTransactionValueAsync(int transactionid, decimal newValue)
        {
            const string query = $"UPDATE bokur_transaction SET value = @{nameof(newValue)} WHERE id = @{nameof(transactionid)} AND external_id IS NULL";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
                await connection.ExecuteAsync(query, new { transactionid, newValue });
        }

        public async Task DeleteTransactionAsync(int transactionId)
        {
            BokurTransaction? transaction = await GetByIdAsync(transactionId);

            if (transaction == null)
                throw new ApiException($"No transaction with id {transactionId} could be found", HttpStatusCode.BadRequest);

            if (transaction.ExternalId != null)
                throw new ApiException($"Transaction with id {transactionId} is a transaction from an external source, can't delete", HttpStatusCode.BadRequest);

            if (transaction.SiblingId != null)
            {
                await RemoveSiblingAsync(transactionId);
                await DeleteSingleTransactionAsync((int)transaction.SiblingId);
            }

            await DeleteSingleTransactionAsync(transactionId);
        }

        private async Task DeleteSingleTransactionAsync(int transactionId)
        {
            const string query = $@"DELETE FROM bokur_transaction WHERE id = @{nameof(transactionId)}";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
                await connection.ExecuteAsync(query, new { transactionId });
        }

        private async Task RemoveSiblingAsync(int transactionId)
        {
            const string query = $@"UPDATE bokur_transaction SET sibling = NULL WHERE id = @{nameof(transactionId)}";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
                await connection.ExecuteAsync(query, new { transactionId });
        }

        private async Task SetSiblingAsync(int transactionId, int siblingId)
        {
            const string query = $@"UPDATE bokur_transaction SET sibling = @{nameof(siblingId)} WHERE id = @{nameof(transactionId)}";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
                await connection.ExecuteAsync(query, new { transactionId, siblingId });
        }

        private async Task SetHasChildrenAsync(int transactionId, bool newValue)
        {
            const string query = $@"UPDATE bokur_transaction SET has_children = @{nameof(newValue)} WHERE id = @{nameof(transactionId)}";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
                await connection.ExecuteAsync(query, new { transactionId, newValue });
        }

        public async Task<BokurTransaction?> GetByIdAsync(int id)
        {
            const string query = @"SELECT * FROM bokur_transaction WHERE id = @Id";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
            {
                BokurTransaction? transaction = await connection.GetSingleOrDefaultAsync<BokurTransaction>(query, new { Id = id },
                new Dictionary<string, Func<object?, Task<object?>>>()
                {
                    { // use a manual parameter lookup for affected account because it's just an id from the database but we want to use a cached object
                        nameof(BokurTransaction.AffectedAccount), async (x) =>
                        {
                            if(x == null) return null;
                            return await _accountRepository.GetByIdAsync((int)x);
                        }
                    }
                });

                if (transaction == null) return null;

                if (transaction.HasChildren)
                    transaction.Children = await GetAllChildrenForParentAsync(connection, transaction.Id);

                return transaction;
            }
        }

        public async Task<List<BokurTransaction>> GetAllThatRequiresActionAsync()
        {
            const string query = @"SELECT * FROM bokur_transaction WHERE
                                   external_id IS NOT NULL AND 
                                   ignored = FALSE
                                   AND ((ignore_file_requirement = FALSE AND associated_file_name IS NULL) OR affected_account IS NULL)";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
                return await connection.GetAsync<BokurTransaction>(query, null, new Dictionary<string, Func<object?, Task<object?>>>()
                {
                    {
                        nameof(BokurTransaction.AffectedAccount), async (x) =>
                        {
                            if(x == null) return null;
                            return await _accountRepository.GetByIdAsync((int)x);
                        }
                    }
                });
        }

        public async Task<List<BokurTransaction>> GetAllChildrenForParentAsync(NpgsqlConnection connection, int parentId)
        {
            const string query = $"SELECT * FROM bokur_transaction WHERE parent = @{nameof(parentId)}";

            return await connection.GetAsync<BokurTransaction>(query, new { parentId }, new Dictionary<string, Func<object?, Task<object?>>>()
            {
                {
                    nameof(BokurTransaction.AffectedAccount), async (x) =>
                    {
                        if(x == null) return null;
                        return await _accountRepository.GetByIdAsync((int)x);
                    }
                }
            });
        }

        public async Task<List<BokurTransaction>> GetAllForMonthAsync(DateTime month)
        {
            DateTime start = new DateTime(month.Year, month.Month, 1);
            DateTime end = start.AddMonths(1).AddDays(-1);

            const string query = @"SELECT * FROM bokur_transaction WHERE
                                   date < @end AND date >= @start";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
            {
                List<BokurTransaction> result = await connection.GetAsync<BokurTransaction>(query, new { start, end },
                new Dictionary<string, Func<object?, Task<object?>>>()
                {
                    {
                        nameof(BokurTransaction.AffectedAccount), async (x) =>
                        {
                            if(x == null) return null;
                            return await _accountRepository.GetByIdAsync((int)x);
                        }
                    }
                });

                foreach (BokurTransaction transaction in result.Where(x => x.HasChildren))
                    transaction.Children = await GetAllChildrenForParentAsync(connection, transaction.Id);

                return result;
            }
        }

        public async Task<List<BokurTransaction>> GetTransactionsForExport(DateTime startDate, DateTime endDate)
        {
            const string query = @$"SELECT * FROM bokur_transaction
                                   WHERE
                                   parent IS NULL AND
                                   sibling IS NULL AND
                                   ignored = FALSE AND
                                   date < @{nameof(endDate)} AND date >= @{nameof(startDate)}
                                   ORDER BY date DESC";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
            {
                List<BokurTransaction> result = await connection.GetAsync<BokurTransaction>(query, new { startDate, endDate },
                new Dictionary<string, Func<object?, Task<object?>>>()
                {
                    {
                        nameof(BokurTransaction.AffectedAccount), async (x) =>
                        {
                            if(x == null) return null;
                            return await _accountRepository.GetByIdAsync((int)x);
                        }
                    }
                });

                return result;
            }
        }

        public async Task<List<BokurTransaction>> GetAllWithoutParentAsync(int pageSize = 10, int page = 0, int? accountId = null)
        {
            string query = @"SELECT * FROM bokur_transaction WHERE parent IS NULL";

            if (accountId.HasValue)
                query += " AND affected_account = @accountId";

            query += @"
                                   ORDER BY date DESC
                                   OFFSET @skip
                                   LIMIT @take";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
            {
                List<BokurTransaction> result = await connection.GetAsync<BokurTransaction>(
                    query,
                    new { skip = page * pageSize, take = pageSize, accountId },
                    new Dictionary<string, Func<object?, Task<object?>>>()
                    {
                        {
                            nameof(BokurTransaction.AffectedAccount), async (x) =>
                            {
                                if(x == null) return null;
                                return await _accountRepository.GetByIdAsync((int)x);
                            }
                        }
                    });

                foreach (BokurTransaction transaction in result.Where(x => x.HasChildren))
                    transaction.Children = await GetAllChildrenForParentAsync(connection, transaction.Id);

                return result;
            }
        }

        public async Task<List<string>> GetExistingExternalIdsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            const string query = @$"SELECT external_id
                                   FROM bokur_transaction
                                   WHERE (@{nameof(startDate)} IS NULL OR date >= @{nameof(startDate)})
                                     AND (@{nameof(endDate)} IS NULL OR date <= @{nameof(endDate)})";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
            {
                return (await connection.GetAsync<string?>(query, new { startDate, endDate })).RemoveNullValues();
            }
        }

        public async Task<List<AccountSummary>> GetSummaryAsync()
        {
            const string query = @"SELECT affected_account AS account, SUM(value) AS balance FROM bokur_transaction
                                   WHERE affected_account IS NOT NULL
                                   AND ignored = FALSE
                                   GROUP BY affected_account
                                   ORDER BY affected_account";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
            {
                List<AccountSummary> result = await connection.GetAsync<AccountSummary>(query, null, new Dictionary<string, Func<object?, Task<object?>>>()
                {
                    {
                        "account", async (x) =>
                        {
                            if(x == null) return null;
                            return await _accountRepository.GetByIdAsync((int)x);
                        }
                    }
                });

                List<BokurAccount> allAccounts = await _accountRepository.GetAllAsync();
                foreach (BokurAccount account in allAccounts)
                {
                    if (result.Any(x => x.Account.Id == account.Id))
                        continue;

                    result.Add(new AccountSummary(account, 0));
                }

                return result;
            }
        }
    }
}
