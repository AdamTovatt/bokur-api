using BokurApi.Managers.Files.Postgres;
using BokurApi.Models.Bokur;
using BokurApiTests.TestUtilities;

namespace BokurApiTests.ManagerTests
{
    [TestClass]
    public class PostgresFileManagerTests
    {
        [TestInitialize]
        public async Task BeforeEach()
        {
            await DatabaseHelper.CleanTable("stored_file");
        }

        [TestMethod]
        public async Task CreateFile()
        {
            bool result = await FileManager.Instance.SaveFileAsync(new BokurFile("TestFile", new byte[100]));

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task GetFile()
        {
            await CreateFile();

            BokurFile? file = await FileManager.Instance.GetFileAsync("TestFile");

            Assert.IsNotNull(file);
            Assert.AreEqual("TestFile", file.Name);
            Assert.AreEqual(100, file.Bytes.Length);
        }

        [TestMethod]
        public async Task FileExists()
        {
            await CreateFile();

            bool result = await FileManager.Instance.FileNameExistsAsync("TestFile");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task DeleteFile()
        {
            await CreateFile();

            bool result = await FileManager.Instance.DeleteFileAsync("TestFile");

            Assert.IsTrue(result);

            result = await FileManager.Instance.FileNameExistsAsync("TestFile");

            Assert.IsFalse(result);
        }
    }
}
