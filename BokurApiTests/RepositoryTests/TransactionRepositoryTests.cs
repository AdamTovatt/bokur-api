using BokurApi.Models.Bokur;
using BokurApi.Repositories;
using BokurApiTests.TestUtilities;

namespace BokurApiTests.RepositoryTests
{
    [TestClass]
    public class TransactionRepositoryTests
    {
        [ClassInitialize]
        public static async Task Setup(TestContext context)
        {
            await AccountRepository.Instance.CreateAsync("Adam");
            await AccountRepository.Instance.CreateAsync("Oliver");
            await AccountRepository.Instance.CreateAsync("Gemensam");
        }

        [ClassCleanup]
        public static async Task Cleanup()
        {
            await DatabaseHelper.CleanTable("bokur_account");
            await DatabaseHelper.CleanTable("bokur_transaction");
        }

        [TestInitialize]
        public async Task BeforeEach()
        {
            await DatabaseHelper.CleanTable("bokur_transaction");
        }

        [TestMethod]
        public async Task Create()
        {
            int createdId = await TransactionRepository.Instance.CreateAsync(TestDataProvider.BokurTransaction1);

            Assert.AreEqual(1, createdId);

            createdId = await TransactionRepository.Instance.CreateAsync(TestDataProvider.BokurTransaction2);

            Assert.AreEqual(2, createdId);
        }

        [TestMethod]
        public async Task GetById()
        {
            await Create();

            BokurTransaction? transaction = await TransactionRepository.Instance.GetByIdAsync(1);

            Assert.IsNotNull(transaction);

            Assert.AreEqual(TestDataProvider.BokurTransaction1.ExternalId, transaction.ExternalId);
            Assert.AreEqual(TestDataProvider.BokurTransaction1.Name, transaction.Name);
            Assert.AreEqual(TestDataProvider.BokurTransaction1.Value, transaction.Value);
            Assert.AreEqual(TestDataProvider.BokurTransaction1.Date, transaction.Date);
        }

        [TestMethod]
        public async Task GetAll()
        {
            await Create();

            List<BokurTransaction> transactions = await TransactionRepository.Instance.GetAllAsync();

            Assert.AreEqual(2, transactions.Count);

            Assert.AreEqual(TestDataProvider.BokurTransaction1.ExternalId, transactions[0].ExternalId);
            Assert.AreEqual(TestDataProvider.BokurTransaction1.Name, transactions[0].Name);
            Assert.AreEqual(TestDataProvider.BokurTransaction1.Value, transactions[0].Value);
            Assert.AreEqual(TestDataProvider.BokurTransaction1.Date, transactions[0].Date);

            Assert.AreEqual(TestDataProvider.BokurTransaction2.ExternalId, transactions[1].ExternalId);
            Assert.AreEqual(TestDataProvider.BokurTransaction2.Name, transactions[1].Name);
            Assert.AreEqual(TestDataProvider.BokurTransaction2.Value, transactions[1].Value);
            Assert.AreEqual(TestDataProvider.BokurTransaction2.Date, transactions[1].Date);
        }

        [TestMethod]
        public async Task Update()
        {
            await Create();

            BokurTransaction? transaction = await TransactionRepository.Instance.GetByIdAsync(1);

            Assert.IsNotNull(transaction);

            transaction.Name = "Updated name";
            transaction.AssociatedFileName = "Updated file name";
            transaction.AffectedAccount = await AccountRepository.Instance.GetByIdAsync(1);
            transaction.Ignored = true;

            await TransactionRepository.Instance.UpdateAsync(transaction);

            transaction = await TransactionRepository.Instance.GetByIdAsync(1);

            Assert.IsNotNull(transaction);

            Assert.AreEqual("Updated name", transaction.Name);
            Assert.AreEqual("Updated file name", transaction.AssociatedFileName);
            Assert.AreEqual(1, transaction.AffectedAccount?.Id);
            Assert.AreEqual(true, transaction.Ignored);

            // Make sure other transactions are not affected
            BokurTransaction? transaction2 = await TransactionRepository.Instance.GetByIdAsync(2);

            Assert.IsNotNull(transaction2);

            Assert.AreEqual(TestDataProvider.BokurTransaction2.Name, transaction2.Name);
        }

        [TestMethod]
        public async Task DeleteAssociatedFile()
        {
            await Update();

            BokurTransaction? transaction = await TransactionRepository.Instance.GetByIdAsync(1);

            Assert.IsNotNull(transaction);

            Assert.AreEqual("Updated file name", transaction.AssociatedFileName);

            await TransactionRepository.Instance.RemoveAssociatedFile(1);

            transaction = await TransactionRepository.Instance.GetByIdAsync(1);

            Assert.IsNotNull(transaction);

            Assert.IsNull(transaction.AssociatedFileName);
        }

        [TestMethod]
        public async Task GetAllExternalIds()
        {
            await Update();

            List<string> externalIds = await TransactionRepository.Instance.GetExistingExternalIdsAsync();

            Assert.AreEqual(2, externalIds.Count);

            Assert.IsTrue(externalIds.Contains(TestDataProvider.BokurTransaction1.ExternalId));
            Assert.IsTrue(externalIds.Contains(TestDataProvider.BokurTransaction2.ExternalId));
        }
    }
}