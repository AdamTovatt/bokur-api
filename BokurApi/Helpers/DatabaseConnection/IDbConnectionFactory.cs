using Npgsql;

namespace BokurApi.Helpers.DatabaseConnection
{
    public interface IDbConnectionFactory
    {
        Task<NpgsqlConnection> GetConnectionAsync();
    }
}