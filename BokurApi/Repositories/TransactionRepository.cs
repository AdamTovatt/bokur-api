using BokurApi.Models.Bokur;
using BokurApi.Helpers;
using Dapper;
using Npgsql;

namespace BokurApi.Repositories
{
    public class TransactionRepository : Repository<TransactionRepository>
    {
        public async Task<int> CreateAsync(BokurTransaction transaction)
        {
            const string query = $@"INSERT INTO bokur_transaction
                                    (external_id, name, value, date) VALUES
                                    (@{nameof(transaction.ExternalId)}, @{nameof(transaction.Name)}, @{nameof(transaction.Value)}, @{nameof(transaction.Date)})
                                    RETURNING id";

            try
            {
                using (NpgsqlConnection connection = await GetConnectionAsync())
                    return await connection.ExecuteScalarAsync<int>(query, new
                    {
                        transaction.ExternalId,
                        transaction.Name,
                        transaction.Value,
                        transaction.Date,
                    });
            }
            catch (PostgresException exception)
            {
                if (exception.SqlState == "23505")
                    throw new ArgumentException($"Transaction with external id {transaction.ExternalId} already exists", exception);
                else
                    throw;
            }
        }

        public async Task UpdateAsync(BokurTransaction transaction)
        {
            if (transaction == null || transaction.Id == 0)
                throw new ArgumentException("Invalid transaction object or missing transaction id");

            if (transaction.Name != null && transaction.Name.Length < 1)
                throw new ArgumentException("Name cannot be less than 1 character");

            const string query = @$"
                UPDATE bokur_transaction
                SET
                    name = COALESCE(@{nameof(transaction.Name)}, name),
                    associated_file_name = COALESCE(@{nameof(transaction.AssociatedFileName)}, associated_file_name),
                    affected_account = COALESCE(@{nameof(transaction.AffectedAccount.Id)}, affected_account),
                    ignored = COALESCE(@{nameof(transaction.Ignored)}, ignored)
                WHERE id = @transactionId";

            using (NpgsqlConnection connection = await GetConnectionAsync())
                await connection.ExecuteAsync(query, new
                {
                    transactionId = transaction.Id,
                    transaction.Name,
                    transaction.AssociatedFileName,
                    transaction.AffectedAccount?.Id,
                    transaction.Ignored,
                });
        }

        public async Task RemoveAssociatedFile(int transactionId)
        {
            const string query = @"UPDATE bokur_transaction SET associated_file_name = NULL WHERE id = @TransactionId";

            using (NpgsqlConnection connection = await GetConnectionAsync())
                await connection.ExecuteAsync(query, new { TransactionId = transactionId });
        }

        public async Task<BokurTransaction?> GetByIdAsync(int id)
        {
            const string query = @"SELECT * FROM bokur_transaction WHERE id = @Id";

            using (NpgsqlConnection connection = await GetConnectionAsync())
            {
                return await connection.GetSingleOrDefaultAsync<BokurTransaction>(query, new { Id = id }, new Dictionary<string, Func<object?, Task<object?>>>()
                {
                    { // use a manual parameter lookup for affected account because it's just an id from the database but we want to use a cached object
                        nameof(BokurTransaction.AffectedAccount), async (x) =>
                        {
                            if(x == null) return null;
                            return await AccountRepository.Instance.GetByIdAsync((int)x);
                        }
                    }
                });
            }
        }

        public async Task<List<BokurTransaction>> GetAllThatRequiresActionAsync()
        {
            const string query = @"SELECT * FROM bokur_transaction WHERE ignored = FALSE AND (associated_file_name IS NULL OR affected_account IS NULL)";

            using (NpgsqlConnection connection = await GetConnectionAsync())
                return await connection.GetAsync<BokurTransaction>(query, null, new Dictionary<string, Func<object?, Task<object?>>>()
                {
                    {
                        nameof(BokurTransaction.AffectedAccount), async (x) =>
                        {
                            if(x == null) return null;
                            return await AccountRepository.Instance.GetByIdAsync((int)x);
                        }
                    }
                });
        }

        public async Task<List<BokurTransaction>> GetAllAsync()
        {
            const string query = @"SELECT * FROM bokur_transaction";

            using (NpgsqlConnection connection = await GetConnectionAsync())
                return await connection.GetAsync<BokurTransaction>(query, null, new Dictionary<string, Func<object?, Task<object?>>>()
                {
                    {
                        nameof(BokurTransaction.AffectedAccount), async (x) =>
                        {
                            if(x == null) return null;
                            return await AccountRepository.Instance.GetByIdAsync((int)x);
                        }
                    }
                });
        }

        public async Task<List<string>> GetExistingExternalIdsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            const string query = @$"SELECT external_id
                                   FROM bokur_transaction
                                   WHERE (@{nameof(startDate)} IS NULL OR date >= @{nameof(startDate)})
                                     AND (@{nameof(endDate)} IS NULL OR date <= @{nameof(endDate)})";

            using (NpgsqlConnection connection = await GetConnectionAsync())
            {
                return (await connection.GetAsync<string>(query, new { startDate, endDate }));
            }
        }
    }
}
