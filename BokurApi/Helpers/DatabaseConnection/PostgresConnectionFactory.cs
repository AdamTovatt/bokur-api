using System.Threading.Tasks;
using Npgsql;

namespace BokurApi.Helpers.DatabaseConnection
{
    public class PostgresConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public PostgresConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public static PostgresConnectionFactory CreateFromEnvironmentVaribles()
        {
            string connectionString = EnvironmentHelper.GetConnectionString();
            return new PostgresConnectionFactory(connectionString);
        }

        public async Task<NpgsqlConnection> GetConnectionAsync()
        {
            NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }
    }
}