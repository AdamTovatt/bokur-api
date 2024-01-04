using BokurApi.Helpers;
using Npgsql;

namespace BokurApi.Repositories
{
    public class Repository<T> where T : new()
    {
        private readonly string connectionString;

        /// <summary>
        /// Will create an instance of the repository
        /// </summary>
        public Repository()
        {
            connectionString = EnvironmentHelper.GetConnectionString();
        }

        /// <summary>
        /// Will return a connection
        /// </summary>
        protected async Task<NpgsqlConnection> GetConnectionAsync()
        {
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private static T? _instance;
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new T();
                    }
                    return _instance;
                }
            }
        }
    }
}
