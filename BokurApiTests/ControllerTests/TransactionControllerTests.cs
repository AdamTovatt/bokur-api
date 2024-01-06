using BokurApi;
using BokurApi.Controllers;
using BokurApi.Models.Bokur;
using BokurApi.Repositories;
using BokurApiTests.TestUtilities;
using Microsoft.AspNetCore.Mvc;

namespace BokurApiTests.ControllerTests
{
    [TestClass]
    public class TransactionControllerTests
    {
        private TransactionController controller = new TransactionController();

        [ClassInitialize]
        public static async Task BeforeAll(TestContext context)
        {
            GlobalSettings.MocketEnvironment = true; // use a mocked environment (affects the FileManager)

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
        }

        [TestInitialize]
        public void BeforeEach()
        {

        }

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
    }
}
