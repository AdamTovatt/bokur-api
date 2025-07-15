using BokurApi.Models.Bokur;
using Dapper;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BokurApi.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private Dictionary<int, BokurAccount>? accountsCache;

        public AccountRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task CreateAsync(string name)
        {
            accountsCache = null; // reset cache

            const string query = $"INSERT INTO bokur_account (name) VALUES (@{nameof(name)})";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
                await connection.ExecuteAsync(query, new { name });
        }

        public async Task SetName(int id, string name)
        {
            accountsCache = null; // reset cache

            const string query = $"UPDATE bokur_account SET name = @{nameof(name)} WHERE id = @{nameof(id)}";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
                await connection.ExecuteAsync(query, new { name, id });
        }

        public async Task<List<BokurAccount>> GetAllAsync()
        {
            const string query = "SELECT * FROM bokur_account";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
            {
                List<BokurAccount> accounts = (await connection.QueryAsync<BokurAccount>(query)).ToList();

                if (accountsCache == null)
                    accountsCache = new Dictionary<int, BokurAccount>();
                else
                    accountsCache.Clear();

                foreach (BokurAccount x in accounts)
                    accountsCache.Add(x.Id, x);

                return accounts;
            }
        }

        public async Task<BokurAccount?> GetByIdAsync(int id)
        {
            if (accountsCache == null)
                await GetAllAsync();

            if (accountsCache == null)
                throw new System.Exception("AccountsCache was still null after getting all accounts, this should not happen");

            if (!accountsCache.ContainsKey(id))
                return null;

            return accountsCache[id];
        }
    }
}
