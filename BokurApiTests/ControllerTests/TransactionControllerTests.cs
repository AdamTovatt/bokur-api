using BokurApi;
using BokurApi.Controllers;
using BokurApi.Managers.Files.Postgres;
using BokurApi.Models.Bokur;
using BokurApi.Repositories;
using BokurApiTests.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BokurApiTests.ControllerTests
{
    [TestClass]
    public class TransactionControllerTests
    {
        private TransactionController controller = new TransactionController();

        [ClassInitialize]
        public static async Task BeforeAll(TestContext context)
        {
            List<BokurTransaction> mockedTransactions = new List<BokurTransaction>()
            {
                TestDataProvider.BokurTransaction1,
                TestDataProvider.BokurTransaction2,
            };

            foreach (BokurTransaction transaction in mockedTransactions)
            {
                await TransactionRepository.Instance.CreateAsync(transaction);
            }
        }

        [ClassCleanup]
        public static async Task AfterAll()
        {
            await DatabaseHelper.CleanTable("bokur_transaction");
            await DatabaseHelper.CleanTable("stored_file");
        }

        [TestInitialize]
        public void BeforeEach() { }

        [TestMethod]
        public async Task GetTransactions()
        {
            ObjectResult objectResult = await controller.GetAll();

            Assert.IsNotNull(objectResult);
            Assert.IsNotNull(objectResult.Value);

            List<BokurTransaction> transactions = (List<BokurTransaction>)objectResult.Value;

            Assert.AreEqual(2, transactions.Count);
        }

        [TestMethod]
        public async Task GetSingleTransaction()
        {
            ObjectResult objectResult = await controller.GetSingle(1);

            Assert.IsNotNull(objectResult);
            Assert.IsNotNull(objectResult.Value);

            BokurTransaction transaction = (BokurTransaction)objectResult.Value;

            Assert.AreEqual(TestDataProvider.BokurTransaction1.ExternalId, transaction.ExternalId);
        }

        [TestMethod]
        public async Task UpdateSingleTransaction()
        {
            const string updatedName = "New, updated name";

            BokurTransaction? transaction = await TransactionRepository.Instance.GetByIdAsync(1);

            Assert.IsNotNull(transaction);

            transaction.Name = updatedName;

            ObjectResult objectResult = await controller.UpdateSingle(transaction);

            Assert.IsNotNull(objectResult);
            Assert.IsNotNull(objectResult.Value);

            transaction = await TransactionRepository.Instance.GetByIdAsync(1);

            Assert.IsNotNull(transaction);

            Assert.AreEqual(updatedName, transaction.Name);
            Assert.AreEqual(TestDataProvider.BokurTransaction1.Value, transaction.Value);
            Assert.AreEqual(TestDataProvider.BokurTransaction1.Date, transaction.Date);
            Assert.AreEqual(TestDataProvider.BokurTransaction1.ExternalId, transaction.ExternalId);
            Assert.AreEqual(TestDataProvider.BokurTransaction1.AffectedAccount, transaction.AffectedAccount);
        }

        [TestMethod]
        public async Task UploadFile()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                byte[] bytes = new byte[100];
                stream.Write(bytes);
                IFormFile file = new FormFile(stream, 0, bytes.Length, "testFile", "testFile.txt");

                ObjectResult objectResult = await controller.UploadFile(file, 1);

                Assert.IsNotNull(objectResult);

                BokurTransaction? transaction = await TransactionRepository.Instance.GetByIdAsync(1);

                Assert.IsNotNull(transaction);

                Assert.AreEqual("testFile.txt", transaction.AssociatedFileName);

                BokurFile? bokurFile = await FileManager.Instance.GetFileAsync("testFile.txt");

                Assert.IsNotNull(bokurFile);

                objectResult = await controller.UploadFile(file, 1);

                Assert.IsNotNull(objectResult);
                Assert.AreEqual((int)HttpStatusCode.Conflict, objectResult.StatusCode);
            }
        }

        [TestMethod]
        public async Task DownloadFile()
        {
            await UploadFile();
            IActionResult result = await controller.DownloadFile(1);

            Assert.IsNotNull(result);

            Assert.IsInstanceOfType(result, typeof(FileResult));

            FileResult fileResult = (FileResult)result;
            Assert.AreEqual("application/octet-stream", fileResult.ContentType);
            Assert.AreEqual("testFile.txt", fileResult.FileDownloadName);
        }
    }
}
