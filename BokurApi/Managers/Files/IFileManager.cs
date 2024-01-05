using BokurApi.Models.Bokur;

namespace BokurApi.Managers.Files
{
    public interface IFileManager
    {
        public Task<BokurFile> GetFileAsync(string fileName);
        public Task<bool> SaveFileAsync(BokurFile file);
        public Task<bool> DeleteFileAsync(string fileName);
        public Task<bool> FileNameExistsAsync(string fileName);
    }
}
