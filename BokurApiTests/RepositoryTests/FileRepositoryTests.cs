using BokurApi.Repositories.File;
using BokurApi.Helpers.DatabaseConnection;
using BokurApiTests.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.Threading.Tasks;
using BokurApi.Models.Exceptions;

namespace BokurApiTests.RepositoryTests
{
    [TestClass]
    public class FileRepositoryTests
    {
        private static IFileRepository _fileRepository = null!;

        [ClassInitialize]
        public static async Task Setup(TestContext testContext)
        {
            string connectionString = BokurApi.Helpers.EnvironmentHelper.GetConnectionString();
            _fileRepository = new FileRepository(new PostgresConnectionFactory(connectionString));

            await DatabaseHelper.CleanTable("stored_file");
        }

        [ClassCleanup]
        public static async Task Cleanup()
        {
            await DatabaseHelper.CleanTable("stored_file");
        }

        [TestMethod]
        public async Task SaveAndReadFileAsync()
        {
            string fileName = "testfile.txt";
            byte[] fileData = Encoding.UTF8.GetBytes("Hello, world!");

            bool saved = await _fileRepository.SaveFileAsync(fileName, fileData);
            Assert.IsTrue(saved);

            byte[]? readData = await _fileRepository.ReadFileAsync(fileName);
            Assert.IsNotNull(readData);
            Assert.AreEqual("Hello, world!", Encoding.UTF8.GetString(readData));
        }

        [TestMethod]
        public async Task DeleteFileAsync_RemovesFile()
        {
            string fileName = "todelete.txt";
            byte[] fileData = Encoding.UTF8.GetBytes("Delete me");

            await _fileRepository.SaveFileAsync(fileName, fileData);
            bool deleted = await _fileRepository.DeleteFileAsync(fileName);
            Assert.IsTrue(deleted);

            byte[]? readData = await _fileRepository.ReadFileAsync(fileName);
            Assert.IsNull(readData);
        }

        [TestMethod]
        public async Task GetFileExists_ReturnsCorrectly()
        {
            string fileName = "exists.txt";
            byte[] fileData = Encoding.UTF8.GetBytes("Exists");

            await _fileRepository.SaveFileAsync(fileName, fileData);
            bool exists = await _fileRepository.GetFileExists(fileName);
            Assert.IsTrue(exists);

            await _fileRepository.DeleteFileAsync(fileName);
            bool stillExists = await _fileRepository.GetFileExists(fileName);
            Assert.IsFalse(stillExists);
        }

        [TestMethod]
        public async Task ReadFileAsync_ReturnsNullForMissingFile()
        {
            byte[]? readData = await _fileRepository.ReadFileAsync("missing.txt");
            Assert.IsNull(readData);
        }

        [TestMethod]
        public async Task SaveFileAsync_DuplicateFileName_ThrowsOrOverwrites()
        {
            string fileName = "duplicate.txt";
            byte[] fileData1 = Encoding.UTF8.GetBytes("First");
            byte[] fileData2 = Encoding.UTF8.GetBytes("Second");

            await _fileRepository.SaveFileAsync(fileName, fileData1);

            ApiException exception = await Assert.ThrowsExceptionAsync<BokurApi.Models.Exceptions.ApiException>(async () =>
            {
                await _fileRepository.SaveFileAsync(fileName, fileData2);
            });

            StringAssert.Contains(exception.Message, $"A file with the name '{fileName}' already exists.");

            byte[]? readData = await _fileRepository.ReadFileAsync(fileName);
            Assert.AreEqual("First", Encoding.UTF8.GetString(readData!));
        }
    }
}