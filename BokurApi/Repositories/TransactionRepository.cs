using BokurApi.Models.Bokur;
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

            using (NpgsqlConnection connection = await GetConnectionAsync())
                return await connection.ExecuteScalarAsync<int>(query, new
                {
                    transaction.ExternalId,
                    transaction.Name,
                    transaction.Value,
                    transaction.Date,
                });
        }

        public async Task UpdateAsync(BokurTransaction transaction)
        {
            if (transaction == null || transaction.Id == 0)
                throw new ArgumentException("Invalid transaction object or missing transaction id");

            const string query = @"
                UPDATE bokur_transaction
                SET
                    name = COALESCE(@Name, name),
                    associated_file_name = COALESCE(@AssociatedFileName, associated_file_name),
                    affected_account = COALESCE(@AffectedAccount, affected_account),
                    ignored = COALESCE(@Ignored, ignored)
                WHERE id = @Id";

            using (NpgsqlConnection connection = await GetConnectionAsync())
                await connection.ExecuteAsync(query, transaction);
        }

        public async Task<BokurTransaction?> GetByIdAsync(int id)
        {
            const string query = @"SELECT * FROM bokur_transaction WHERE id = @Id";

            using (NpgsqlConnection connection = await GetConnectionAsync())
                return await connection.QuerySingleOrDefaultAsync<BokurTransaction>(query, new { Id = id });
        }

        public async Task<List<string>> GetExistingExternalIdsAsync(DateTime? startDate, DateTime? endDate)
        {
            const string query = @"SELECT external_id
                                   FROM bokur_transaction
                                   WHERE date >= @StartDate AND date <= @EndDate";

            using (NpgsqlConnection connection = await GetConnectionAsync())
            {
                return (await connection.QueryAsync<string>(query, new
                {
                    StartDate = startDate ?? DateTime.MinValue,
                    EndDate = endDate ?? DateTime.MaxValue
                })).ToList();
            }
        }
    }
}
