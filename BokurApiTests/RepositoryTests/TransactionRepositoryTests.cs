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
            await DatabaseHelper.CleanTable("bokur_account");
            await DatabaseHelper.CleanTable("bokur_transaction");
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
        public async Task GetAllThatRequiresAction()
        {
            await Create();

            List<BokurTransaction> transactions = await TransactionRepository.Instance.GetAllThatRequiresActionAsync();

            Assert.AreEqual(2, transactions.Count);

            BokurTransaction transaction = transactions[0];

            transaction.Name = "Updated name";
            transaction.AssociatedFileName = "Updated file name";
            transaction.AffectedAccount = await AccountRepository.Instance.GetByIdAsync(1);

            await TransactionRepository.Instance.UpdateAsync(transaction);

            transactions = await TransactionRepository.Instance.GetAllThatRequiresActionAsync();

            Assert.AreEqual(1, transactions.Count);
        }

        [TestMethod]
        public async Task CreateTransferAndGetChildren()
        {
            await Create();

            BokurTransaction? transaction = await TransactionRepository.Instance.GetByIdAsync(1);

            Assert.IsNotNull(transaction);
            Assert.IsFalse(transaction.HasChildren);

            await TransactionRepository.Instance.SetAffectedAccountAsync(1, 3);

            bool success = await TransactionRepository.Instance.CreateTransferAsync(1, 2, 100);

            Assert.IsTrue(success);

            transaction = await TransactionRepository.Instance.GetByIdAsync(1);

            Assert.IsNotNull(transaction);
            Assert.AreEqual(1, transaction.Id);
            Assert.IsTrue(transaction.HasChildren);

            Assert.AreEqual(2, transaction.Children?.Count);

            success = await TransactionRepository.Instance.CreateTransferAsync(1, 1, 100);

            Assert.IsTrue(success);

            transaction = await TransactionRepository.Instance.GetByIdAsync(1);

            Assert.IsNotNull(transaction);
            Assert.AreEqual(1, transaction.Id);
            Assert.IsTrue(transaction.HasChildren);

            Assert.AreEqual(4, transaction.Children?.Count);

            List<BokurTransaction> transactions = await TransactionRepository.Instance.GetAllAsync();

            Assert.AreEqual(2, transactions.Count);
            Assert.IsTrue(transactions[0].HasChildren);
            Assert.IsFalse(transactions[1].HasChildren);
        }

        [TestMethod]
        public async Task GetAccountSummaryWithChildTransfers()
        {
            await TransactionRepository.Instance.CreateAsync(TestDataProvider.BokurTransaction1);
            await TransactionRepository.Instance.CreateAsync(TestDataProvider.BokurTransaction2);
            await TransactionRepository.Instance.CreateAsync(TestDataProvider.BokurTransaction3);

            List<AccountSummary> accountSummaries = await TransactionRepository.Instance.GetSummaryAsync();

            Assert.AreEqual(3, accountSummaries.Count);

            foreach(AccountSummary summary in accountSummaries)
                Assert.AreEqual(0, summary.Balance);

            await TransactionRepository.Instance.SetAffectedAccountAsync(1, 1);
            await TransactionRepository.Instance.SetAffectedAccountAsync(2, 2);
            await TransactionRepository.Instance.SetAffectedAccountAsync(3, 3);

            accountSummaries = await TransactionRepository.Instance.GetSummaryAsync();

            Assert.AreEqual(3, accountSummaries.Count);

            Assert.AreEqual(-21.34m, accountSummaries[0].Balance);
            Assert.AreEqual(20000, accountSummaries[1].Balance);
            Assert.AreEqual(-100, accountSummaries[2].Balance);

            await TransactionRepository.Instance.CreateTransferAsync(3, accountSummaries[0].Account.Id, 400);
            await TransactionRepository.Instance.CreateTransferAsync(3, accountSummaries[2].Account.Id, 200);

            accountSummaries = await TransactionRepository.Instance.GetSummaryAsync();

            Assert.AreEqual(3, accountSummaries.Count);

            Assert.AreEqual(378.66m, accountSummaries[0].Balance);
            Assert.AreEqual(19400, accountSummaries[1].Balance);
            Assert.AreEqual(100, accountSummaries[2].Balance);
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

            Assert.IsTrue(externalIds.Contains(TestDataProvider.BokurTransaction1.ExternalId!));
            Assert.IsTrue(externalIds.Contains(TestDataProvider.BokurTransaction2.ExternalId!));
        }
    }
}