using BokurApi.Managers.Files.Postgres;
using BokurApi.Models.Bokur;
using BokurApiTests.TestUtilities;
using BokurApi.Repositories.File;
using BokurApi.Helpers.DatabaseConnection;

namespace BokurApiTests.ManagerTests
{
    [TestClass]
    public class PostgresFileManagerTests
    {
        private PostgresFileManager _fileManager = null!;

        [TestInitialize]
        public async Task BeforeEach()
        {
            await DatabaseHelper.CleanTable("stored_file");
            IFileRepository fileRepository = new FileRepository(PostgresConnectionFactory.CreateFromEnvironmentVaribles());
            _fileManager = new PostgresFileManager(fileRepository);
        }

        [TestMethod]
        public async Task CreateFile()
        {
            bool result = await _fileManager.SaveFileAsync(new BokurFile("TestFile", new byte[100]));

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task GetFile()
        {
            await CreateFile();

            BokurFile? file = await _fileManager.GetFileAsync("TestFile");

            Assert.IsNotNull(file);
            Assert.AreEqual("TestFile", file.Name);
            Assert.AreEqual(100, file.Bytes.Length);
        }

        [TestMethod]
        public async Task FileExists()
        {
            await CreateFile();

            bool result = await _fileManager.FileNameExistsAsync("TestFile");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task DeleteFile()
        {
            await CreateFile();

            bool result = await _fileManager.DeleteFileAsync("TestFile");

            Assert.IsTrue(result);

            result = await _fileManager.FileNameExistsAsync("TestFile");

            Assert.IsFalse(result);
        }
    }
}
