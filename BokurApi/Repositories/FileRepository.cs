﻿using BokurApi.Helpers;
using Dapper;
using Npgsql;

namespace BokurApi.Repositories
{
    public class FileRepository : Repository<FileRepository>
    {
        public async Task<byte[]?> ReadFileAsync(string fileName)
        {
            const string query = $@"
                SELECT content
                FROM stored_file
                WHERE name = @{nameof(fileName)}";

            using (NpgsqlConnection connection = await GetConnectionAsync())
                return await connection.GetSingleOrDefaultAsync<byte[]>(query, new { fileName });
        }

        public async Task<bool> SaveFileAsync(string fileName, byte[] fileData)
        {
            const string query = $@"
                INSERT INTO stored_file (name, content)
                VALUES (@{nameof(fileName)}, @{nameof(fileData)})";

            using (NpgsqlConnection connection = await GetConnectionAsync())
                return (await connection.ExecuteAsync(query, new { fileName, fileData })) > 0;
        }

        public async Task<bool> DeleteFileAsync(string fileName)
        {
            const string query = $@"
                DELETE FROM stored_file
                WHERE name = @{nameof(fileName)}";

            using (NpgsqlConnection connection = await GetConnectionAsync())
                return (await connection.ExecuteAsync(query, new { fileName })) > 0;
        }

        public async Task<bool> GetFileExists(string fileName)
        {
            const string query = $@"
                SELECT EXISTS(
                    SELECT 1
                    FROM stored_file
                    WHERE name = @{nameof(fileName)}
                )";

            using (NpgsqlConnection connection = await GetConnectionAsync())
                return await connection.ExecuteScalarAsync<bool>(query, new { fileName });
        }
    }
}
