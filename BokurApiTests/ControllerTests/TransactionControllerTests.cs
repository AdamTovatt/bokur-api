using BokurApi.Controllers;
using BokurApi.Helpers;
using BokurApi.Managers.Files;
using BokurApi.Managers.Files.Postgres;
using BokurApi.Models.Bokur;
using BokurApi.Repositories;
using BokurApiTests.InMemoryRepositories;
using BokurApiTests.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BokurApiTests.ControllerTests
{
    [TestClass]
    public class TransactionControllerTests
    {
        private static TransactionController _controller = null!;
        private static InMemoryTransactionRepository _transactionRepository = null!;
        private static InMemoryAccountRepository _accountRepository = null!;
        private static InMemoryFileManager _fileManager = null!;

        [ClassInitialize]
        public static async Task BeforeAll(TestContext context)
        {
            _accountRepository = new InMemoryAccountRepository();
            _fileManager = new InMemoryFileManager();
            _transactionRepository = new InMemoryTransactionRepository();
            _controller = new TransactionController(_transactionRepository, _accountRepository, _fileManager);

            // Add test data
            await _accountRepository.CreateAsync("Adam");
            await _accountRepository.CreateAsync("Oliver");
            await _accountRepository.CreateAsync("Shared");

            await _transactionRepository.CreateAsync(TestDataProvider.BokurTransaction1);
            await _transactionRepository.CreateAsync(TestDataProvider.BokurTransaction2);
        }

        [TestMethod]
        public async Task GetTransactions()
        {
            ObjectResult objectResult = await _controller.GetAll();

            Assert.IsNotNull(objectResult);
            Assert.IsNotNull(objectResult.Value);

            List<BokurTransaction> transactions = (List<BokurTransaction>)objectResult.Value;

            Assert.AreEqual(2, transactions.Count);
        }

        [TestMethod]
        public async Task GetSingleTransaction()
        {
            ObjectResult objectResult = await _controller.GetSingle(1);

            Assert.IsNotNull(objectResult);
            Assert.IsNotNull(objectResult.Value);

            BokurTransaction transaction = (BokurTransaction)objectResult.Value;

            Assert.AreEqual(TestDataProvider.BokurTransaction1.ExternalId, transaction.ExternalId);
        }

        [TestMethod]
        public async Task UpdateSingleTransaction()
        {
            const string updatedName = "New, updated name";

            BokurTransaction? transaction = await _transactionRepository.GetByIdAsync(1);

            Assert.IsNotNull(transaction);

            transaction.Name = updatedName;

            ObjectResult objectResult = await _controller.UpdateSingle(transaction);

            Assert.IsNotNull(objectResult);
            Assert.IsNotNull(objectResult.Value);

            transaction = await _transactionRepository.GetByIdAsync(1);

            Assert.IsNotNull(transaction);

            Assert.AreEqual(updatedName, transaction.Name);
            Assert.AreEqual(TestDataProvider.BokurTransaction1.Value, transaction.Value);
            Assert.AreEqual(TestDataProvider.BokurTransaction1.Date, transaction.Date);
            Assert.AreEqual(TestDataProvider.BokurTransaction1.ExternalId, transaction.ExternalId);
            Assert.AreEqual(TestDataProvider.BokurTransaction1.AffectedAccount, transaction.AffectedAccount);

            AccountController accountController = new AccountController(new SummaryHelper(_transactionRepository), _accountRepository);

            ObjectResult accountsResult = await accountController.GetAllAccounts();

            Assert.IsNotNull(accountsResult);
            Assert.IsNotNull(accountsResult.Value);

            List<BokurAccount> accounts = (List<BokurAccount>)accountsResult.Value;

            Assert.AreEqual(3, accounts.Count);

            transaction.AffectedAccount = accounts.First(x => x.Name == "Oliver");

            objectResult = await _controller.UpdateSingle(transaction);

            Assert.IsNotNull(objectResult);

            transaction = await _transactionRepository.GetByIdAsync(1);

            Assert.IsNotNull(transaction);

            Assert.AreEqual("Oliver", transaction.AffectedAccount?.Name);
        }

        [TestMethod]
        public async Task UploadFile()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                byte[] bytes = new byte[100];
                stream.Write(bytes);
                stream.Position = 0; // Ensure stream is readable from the start
                IFormFile file = new FormFile(stream, 0, bytes.Length, "testFile", "testFile.txt");

                ObjectResult objectResult = await _controller.UploadTransactionFile(file, 1);

                Assert.IsNotNull(objectResult);

                BokurTransaction? transaction = await _transactionRepository.GetByIdAsync(1);

                Assert.IsNotNull(transaction);

                Assert.AreEqual("testFile.txt", transaction.AssociatedFileName);

                BokurFile? bokurFile = await _fileManager.GetFileAsync("testFile.txt");

                Assert.IsNotNull(bokurFile);

                objectResult = await _controller.UploadTransactionFile(file, 1);

                Assert.IsNotNull(objectResult);
                Assert.AreEqual((int)HttpStatusCode.BadRequest, objectResult.StatusCode);
            }
        }

        [TestMethod]
        public async Task DeleteFile()
        {
            await UploadFile();

            ObjectResult objectResult = await _controller.DeleteTransactionFile(1);

            Assert.IsNotNull(objectResult);

            BokurTransaction? transaction = await _transactionRepository.GetByIdAsync(1);

            Assert.IsNotNull(transaction);

            Assert.IsNull(transaction.AssociatedFileName);

            BokurFile? bokurFile = await _fileManager.GetFileAsync("testFile.txt");
            Assert.IsNull(bokurFile);
        }

        [TestMethod]
        public async Task DownloadFile()
        {
            await UploadFile();
            IActionResult result = await _controller.DownloadTransactionFile(1);

            Assert.IsNotNull(result);

            Assert.IsInstanceOfType(result, typeof(FileResult));

            FileResult fileResult = (FileResult)result;
            Assert.AreEqual("application/octet-stream", fileResult.ContentType);
            Assert.AreEqual("testFile.txt", fileResult.FileDownloadName);
        }
    }
}
