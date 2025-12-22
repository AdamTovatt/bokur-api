using BokurApi.Repositories.File;

namespace BokurApiTests.InMemoryRepositories
{
    public class InMemoryFileRepository : IFileRepository
    {
        private readonly Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();

        public Task<byte[]?> ReadFileAsync(string fileName)
        {
            files.TryGetValue(fileName, out byte[]? fileBytes);
            return Task.FromResult(fileBytes);
        }

        public Task<bool> SaveFileAsync(string fileName, byte[] fileData)
        {
            files[fileName] = fileData;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteFileAsync(string fileName)
        {
            return Task.FromResult(files.Remove(fileName));
        }

        public Task<bool> GetFileExists(string fileName)
        {
            return Task.FromResult(files.ContainsKey(fileName));
        }
    }
}

