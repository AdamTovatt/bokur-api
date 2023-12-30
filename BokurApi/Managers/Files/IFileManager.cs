using BokurApi.Models;

namespace BokurApi.Managers.Files
{
    public interface IFileManager
    {
        public BokurFile GetFile(string fileName);
        public void SaveFile(string fileName);
        public void DeleteFile(string fileName);
    }
}
