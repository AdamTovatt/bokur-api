using System.Threading.Tasks;

namespace BokurApi.Repositories
{
    public interface IFileRepository
    {
        Task<byte[]?> ReadFileAsync(string fileName);
        Task<bool> SaveFileAsync(string fileName, byte[] fileData);
        Task<bool> DeleteFileAsync(string fileName);
        Task<bool> GetFileExists(string fileName);
    }
} 