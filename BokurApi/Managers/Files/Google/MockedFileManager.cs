using BokurApi.Models.Bokur;

namespace BokurApi.Managers.Files.Google
{
    internal class MockedFileManager : IFileManager
    {
        public async Task<bool> DeleteFileAsync(string fileName)
        {
            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> FileNameExistsAsync(string fileName)
        {
            await Task.CompletedTask;
            return true;
        }

        public async Task<BokurFile> GetFileAsync(string fileName)
        {
            await Task.CompletedTask;
            return new BokurFile("Mocked name", new byte[100]);
        }

        public async Task<bool> SaveFileAsync(BokurFile file)
        {
            await Task.CompletedTask;
            return true;
        }
    }
}
