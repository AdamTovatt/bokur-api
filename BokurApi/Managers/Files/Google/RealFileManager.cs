using BokurApi.Models.Bokur;

namespace BokurApi.Managers.Files.Google
{
    internal class RealFileManager : IFileManager
    {
        public async Task<bool> DeleteFileAsync(string fileName)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> FileNameExistsAsync(string fileName)
        {
            throw new NotImplementedException();
        }

        public async Task<BokurFile> GetFileAsync(string fileName)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> SaveFileAsync(BokurFile file)
        {
            throw new NotImplementedException();
        }
    }
}
