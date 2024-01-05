namespace BokurApi.Managers.Files.Google
{
    public class FileManager
    {
        private static IFileManager? _instance;
        public static IFileManager Instance
        {
            get
            {
                if(_instance == null)
                {
                    if (GlobalSettings.MocketEnvironment)
                        _instance = new MockedFileManager();
                    else
                        _instance = new RealFileManager();
                }

                return _instance;
            }
        }
    }
}
