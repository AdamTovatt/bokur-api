using BokurApi.Models.Bokur;
using BokurApi.Repositories.File;

namespace BokurApi.Managers.Files.Postgres
{
    public class PostgresFileManager : IFileManager
    {
        private readonly IFileRepository _fileRepository;

        public PostgresFileManager(IFileRepository fileRepository)
        {
            _fileRepository = fileRepository;
        }

        public async Task<bool> DeleteFileAsync(string fileName)
        {
            return await _fileRepository.DeleteFileAsync(fileName);
        }

        public async Task<bool> FileNameExistsAsync(string fileName)
        {
            return await _fileRepository.GetFileExists(fileName);
        }

        public async Task<BokurFile?> GetFileAsync(string fileName)
        {
            byte[]? bytes = await _fileRepository.ReadFileAsync(fileName);

            if (bytes == null)
                return null;

            return new BokurFile(fileName, bytes);
        }

        public async Task<bool> SaveFileAsync(BokurFile file)
        {
            return await _fileRepository.SaveFileAsync(file.Name, file.Bytes);
        }
    }
}
