using BokurApi.Helpers;
using BokurApi.Helpers.DatabaseConnection;
using Dapper;
using Npgsql;
using BokurApi.Models.Exceptions;
using System.Net;

namespace BokurApi.Repositories.File
{
    public class FileRepository : IFileRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public FileRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<byte[]?> ReadFileAsync(string fileName)
        {
            const string query = $@"
                SELECT content
                FROM stored_file
                WHERE name = @{nameof(fileName)}";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
                return await connection.GetSingleOrDefaultAsync<byte[]>(query, new { fileName });
        }

        public async Task<bool> SaveFileAsync(string fileName, byte[] fileData)
        {
            const string query = $@"
                INSERT INTO stored_file (name, content)
                VALUES (@{nameof(fileName)}, @{nameof(fileData)})";

            try
            {
                using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
                    return await connection.ExecuteAsync(query, new { fileName, fileData }) > 0;
            }
            catch (PostgresException exception)
            {
                if (exception.SqlState == "23505")
                    throw new ApiException($"A file with the name '{fileName}' already exists.", HttpStatusCode.BadRequest);
                else
                    throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string fileName)
        {
            const string query = $@"
                DELETE FROM stored_file
                WHERE name = @{nameof(fileName)}";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
                return await connection.ExecuteAsync(query, new { fileName }) > 0;
        }

        public async Task<bool> GetFileExists(string fileName)
        {
            const string query = $@"
                SELECT EXISTS(
                    SELECT 1
                    FROM stored_file
                    WHERE name = @{nameof(fileName)}
                )";

            using (NpgsqlConnection connection = await _connectionFactory.GetConnectionAsync())
                return await connection.ExecuteScalarAsync<bool>(query, new { fileName });
        }
    }
}
