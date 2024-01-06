using BokurApi.Models.Bokur;
using BokurApi.Repositories;

namespace BokurApi.Managers.Files.Postgres
{
    internal class RealFileManager : IFileManager
    {
        public async Task<bool> DeleteFileAsync(string fileName)
        {
            return await FileRepository.Instance.DeleteFileAsync(fileName);
        }

        public async Task<bool> FileNameExistsAsync(string fileName)
        {
            return await FileRepository.Instance.GetFileExists(fileName);
        }

        public async Task<BokurFile?> GetFileAsync(string fileName)
        {
            byte[]? bytes = await FileRepository.Instance.ReadFileAsync(fileName);

            if (bytes == null)
                return null;

            return new BokurFile(fileName, bytes);
        }

        public async Task<bool> SaveFileAsync(BokurFile file)
        {
            return await FileRepository.Instance.SaveFileAsync(file.Name, file.Bytes);
        }
    }
}
