using BokurApi.Models.Bokur;
using Dapper;
using Npgsql;

namespace BokurApi.Repositories
{
    public class AccountRepository : Repository<AccountRepository>
    {
        private Dictionary<int, BokurAccount>? accountsCache;

        public async Task CreateAsync(string name)
        {
            accountsCache = null; // reset cache

            const string query = $"INSERT INTO bokur_account (name) VALUES (@{nameof(name)})";

            using (NpgsqlConnection connection = await GetConnectionAsync())
                await connection.ExecuteAsync(query, new { name });
        }

        public async Task SetName(int id, string name)
        {
            accountsCache = null; // reset cache

            const string query = $"UPDATE bokur_account SET name = @{nameof(name)} WHERE id = @{nameof(id)}";

            using (NpgsqlConnection connection = await GetConnectionAsync())
                await connection.ExecuteAsync(query, new { name, id });
        }

        public async Task<List<BokurAccount>> GetAllAsync()
        {
            const string query = "SELECT * FROM bokur_account";

            using (NpgsqlConnection connection = await GetConnectionAsync())
            {
                List<BokurAccount> accounts = (await connection.QueryAsync<BokurAccount>(query)).ToList();

                if (accountsCache == null)
                    accountsCache = new Dictionary<int, BokurAccount>();
                else
                    accountsCache.Clear();

                accounts.ForEach(x => accountsCache.Add(x.Id, x));

                return accounts;
            }
        }

        public async Task<BokurAccount> GetByIdAsync(int id)
        {
            if(accountsCache == null)
                await GetAllAsync();

            if (accountsCache == null)
                throw new Exception("AccountsCache was still nul after getting all accounts, this should not happen");

            return accountsCache[id];
        }
    }
}
