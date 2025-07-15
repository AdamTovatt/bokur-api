using System.Threading.Tasks;
using Npgsql;

namespace BokurApi.Repositories
{
    public interface IDbConnectionFactory
    {
        Task<NpgsqlConnection> GetConnectionAsync();
    }
} 