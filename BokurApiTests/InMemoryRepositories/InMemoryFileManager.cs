using BokurApi.Managers.Files;
using BokurApi.Models.Bokur;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BokurApiTests.InMemoryRepositories
{
    public class InMemoryFileManager : IFileManager
    {
        private readonly Dictionary<string, BokurFile> files = new Dictionary<string, BokurFile>();

        public Task<bool> DeleteFileAsync(string fileName)
        {
            return Task.FromResult(files.Remove(fileName));
        }

        public Task<bool> FileNameExistsAsync(string fileName)
        {
            return Task.FromResult(files.ContainsKey(fileName));
        }

        public Task<BokurFile?> GetFileAsync(string fileName)
        {
            files.TryGetValue(fileName, out BokurFile? file);
            return Task.FromResult(file);
        }

        public Task<bool> SaveFileAsync(BokurFile file)
        {
            files[file.Name] = file;
            return Task.FromResult(true);
        }
    }
}