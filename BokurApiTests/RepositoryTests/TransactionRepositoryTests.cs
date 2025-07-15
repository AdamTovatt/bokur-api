using BokurApi;
using BokurApi.Helpers.DatabaseConnection;
using BokurApi.Managers.Emails;
using BokurApi.Models.Bokur;
using BokurApi.Repositories;
using BokurApi.Repositories.Account;
using BokurApi.Repositories.Transaction;
using BokurApiTests.TestUtilities;

namespace BokurApiTests.RepositoryTests
{
    [TestClass]
    public class TransactionRepositoryTests
    {
        private ITransactionRepository _transactionRepository = null!;
        private static IAccountRepository _accountRepository = null!;

        [ClassInitialize]
        public static async Task Setup(TestContext context)
        {
            await DatabaseHelper.CleanTable("bokur_account");
            await DatabaseHelper.CleanTable("bokur_transaction");

            string connectionString = BokurApi.Helpers.EnvironmentHelper.GetConnectionString();
            _accountRepository = new AccountRepository(new PostgresConnectionFactory(connectionString));

            await _accountRepository.CreateAsync("Adam");
            await _accountRepository.CreateAsync("Oliver");
            await _accountRepository.CreateAsync("Shared");
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
            _transactionRepository = new TransactionRepository(
                new PostgresConnectionFactory(BokurApi.Helpers.EnvironmentHelper.GetConnectionString()),
                _accountRepository
            );
        }

        [TestMethod]
        public async Task Create()
        {
            int createdId = await _transactionRepository.CreateAsync(TestDataProvider.BokurTransaction1);

            Assert.AreEqual(1, createdId);

            createdId = await _transactionRepository.CreateAsync(TestDataProvider.BokurTransaction2);

            Assert.AreEqual(2, createdId);
        }

        [TestMethod]
        public async Task GetById()
        {
            await Create();

            BokurTransaction? transaction = await _transactionRepository.GetByIdAsync(1);

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

            List<BokurTransaction> transactions = await _transactionRepository.GetAllWithoutParentAsync();

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
        public async Task GetAllForMonth()
        {
            await Create();

            List<BokurTransaction> transactions = await _transactionRepository.GetAllForMonthAsync(TestDataProvider.BokurTransaction1.Date);

            Assert.AreEqual(1, transactions.Count);

            Assert.AreEqual(TestDataProvider.BokurTransaction1.ExternalId, transactions[0].ExternalId);

            transactions = await _transactionRepository.GetAllForMonthAsync(TestDataProvider.BokurTransaction2.Date);

            Assert.AreEqual(1, transactions.Count);

            Assert.AreEqual(TestDataProvider.BokurTransaction2.ExternalId, transactions[0].ExternalId);
        }

        [TestMethod]
        public async Task GetAllThatRequiresAction()
        {
            await Create();

            List<BokurTransaction> transactions = await _transactionRepository.GetAllThatRequiresActionAsync();

            Assert.AreEqual(2, transactions.Count);

            BokurTransaction transaction = transactions[0];

            transaction.Name = "Updated name";
            transaction.AssociatedFileName = "Updated file name";
            transaction.AffectedAccount = await _accountRepository.GetByIdAsync(1);

            await _transactionRepository.UpdateAsync(transaction);

            transactions = await _transactionRepository.GetAllThatRequiresActionAsync();

            Assert.AreEqual(1, transactions.Count);

            transaction = transactions[0];

            transaction.AffectedAccount = await _accountRepository.GetByIdAsync(2);
            transaction.IgnoreFileRequirement = true;

            await _transactionRepository.UpdateAsync(transaction);

            transactions = await _transactionRepository.GetAllThatRequiresActionAsync();

            Assert.AreEqual(0, transactions.Count);
        }

        [TestMethod]
        public async Task CreateTransferAndGetChildren()
        {
            await Create();

            BokurTransaction? transaction = await _transactionRepository.GetByIdAsync(1);

            Assert.IsNotNull(transaction);
            Assert.IsFalse(transaction.HasChildren);

            await _transactionRepository.SetAffectedAccountAsync(1, 3);

            bool success = await _transactionRepository.CreateTransferAsync(1, 2, 100);

            Assert.IsTrue(success);

            transaction = await _transactionRepository.GetByIdAsync(1);

            Assert.IsNotNull(transaction);
            Assert.AreEqual(1, transaction.Id);
            Assert.IsTrue(transaction.HasChildren);

            Assert.AreEqual(2, transaction.Children?.Count);

            success = await _transactionRepository.CreateTransferAsync(1, 1, 100);

            Assert.IsTrue(success);

            transaction = await _transactionRepository.GetByIdAsync(1);

            Assert.IsNotNull(transaction);
            Assert.AreEqual(1, transaction.Id);
            Assert.IsTrue(transaction.HasChildren);

            Assert.AreEqual(4, transaction.Children?.Count);

            List<BokurTransaction> transactions = await _transactionRepository.GetAllWithoutParentAsync();

            Assert.AreEqual(2, transactions.Count);
            Assert.IsTrue(transactions[0].HasChildren);
            Assert.IsFalse(transactions[1].HasChildren);
        }

        [TestMethod]
        public async Task GetAccountSummaryWithChildTransfers()
        {
            await _transactionRepository.CreateAsync(TestDataProvider.BokurTransaction1); // take 100
            await _transactionRepository.CreateAsync(TestDataProvider.BokurTransaction2); // take 21.34
            await _transactionRepository.CreateAsync(TestDataProvider.BokurTransaction3); // give 20000

            List<AccountSummary> accountSummaries = await _transactionRepository.GetSummaryAsync();

            Assert.AreEqual(3, accountSummaries.Count);

            foreach (AccountSummary summary in accountSummaries)
                Assert.AreEqual(0, summary.Balance);

            await _transactionRepository.SetAffectedAccountAsync(1, 1); // assign the take 100 to Adam
            await _transactionRepository.SetAffectedAccountAsync(2, 2); // assign the take 21.34 to Oliver
            await _transactionRepository.SetAffectedAccountAsync(3, 3); // assign the give 20000 to Shared

            accountSummaries = await _transactionRepository.GetSummaryAsync();

            Assert.AreEqual(3, accountSummaries.Count);

            Assert.AreEqual(-100, accountSummaries.TakeByAccount("Adam").Balance); // check that Adam has -100
            Assert.AreEqual(-21.34m, accountSummaries.TakeByAccount("Oliver").Balance); // check that Oliver has -21.34
            Assert.AreEqual(20000, accountSummaries.TakeByAccount("Shared").Balance); // check that Shared has 20000

            await _transactionRepository.CreateTransferAsync(3, accountSummaries.TakeAccountId("Oliver"), 400); // give 400 to Oliver
            await _transactionRepository.CreateTransferAsync(3, accountSummaries.TakeAccountId("Adam"), 200); // give 200 to Adam       (both from Shared)
            // the above two lines should mean Shared lost 600

            accountSummaries = await _transactionRepository.GetSummaryAsync();

            Assert.AreEqual(3, accountSummaries.Count);

            Assert.AreEqual(378.66m, accountSummaries.TakeByAccount("Oliver").Balance); // check that Oliver has -21.34 + 400 = 378.66
            Assert.AreEqual(19400, accountSummaries.TakeByAccount("Shared").Balance); // check that Shared has 20000 - 600 = 19400
            Assert.AreEqual(100, accountSummaries.TakeByAccount("Adam").Balance); // check that Adam has -100 + 200 = 100

            await _transactionRepository.CreateTransferAsync(3, accountSummaries.TakeAccountId("Oliver"), 576); // give 576 to Oliver from Shared

            accountSummaries = await _transactionRepository.GetSummaryAsync();
            Assert.AreEqual(3, accountSummaries.Count);

            // Oliver had 378.66, so now he should have 378.66 + 576 = 954.66

            Assert.AreEqual(954.66m, accountSummaries.TakeByAccount("Oliver").Balance); // check that Oliver has 954.66
            Assert.AreEqual(18824, accountSummaries.TakeByAccount("Shared").Balance); // check that Shared has 19400 - 576 = 18824
            Assert.AreEqual(100, accountSummaries.TakeByAccount("Adam").Balance); // check that Adam has 100

            BokurTransaction? transaction = await _transactionRepository.GetByIdAsync(3);

            Assert.IsNotNull(transaction);
            Assert.IsNotNull(transaction.Children);

            BokurTransaction singleSibling = transaction.Children.Last();

            Assert.AreEqual(576, singleSibling.Value); // find the transaction that added 576 to Oliver

            await _transactionRepository.DeleteTransactionAsync(singleSibling.Id);

            accountSummaries = await _transactionRepository.GetSummaryAsync();

            Assert.AreEqual(3, accountSummaries.Count);

            Assert.AreEqual(378.66m, accountSummaries.TakeByAccount("Oliver").Balance);
            Assert.AreEqual(19400, accountSummaries.TakeByAccount("Shared").Balance);
            Assert.AreEqual(100, accountSummaries.TakeByAccount("Adam").Balance);

            BokurTransaction? transaction2 = await _transactionRepository.GetByIdAsync(3);

            Assert.IsNotNull(transaction2);
            Assert.IsNotNull(transaction2.Children);

            Assert.AreEqual(transaction2.Children.Count, transaction.Children.Count - 2);
        }

        [TestMethod]
        public async Task Update()
        {
            await Create();

            BokurTransaction? transaction = await _transactionRepository.GetByIdAsync(1);

            Assert.IsNotNull(transaction);

            transaction.Name = "Updated name";
            transaction.AssociatedFileName = "Updated file name";
            transaction.AffectedAccount = await _accountRepository.GetByIdAsync(1);
            transaction.Ignored = true;

            await _transactionRepository.UpdateAsync(transaction);

            transaction = await _transactionRepository.GetByIdAsync(1);

            Assert.IsNotNull(transaction);

            Assert.AreEqual("Updated name", transaction.Name);
            Assert.AreEqual("Updated file name", transaction.AssociatedFileName);
            Assert.AreEqual(1, transaction.AffectedAccount?.Id);
            Assert.AreEqual(true, transaction.Ignored);

            // Make sure other transactions are not affected
            BokurTransaction? transaction2 = await _transactionRepository.GetByIdAsync(2);

            Assert.IsNotNull(transaction2);

            Assert.AreEqual(TestDataProvider.BokurTransaction2.Name, transaction2.Name);
        }

        [TestMethod]
        public async Task DeleteAssociatedFile()
        {
            await Update();

            BokurTransaction? transaction = await _transactionRepository.GetByIdAsync(1);

            Assert.IsNotNull(transaction);

            Assert.AreEqual("Updated file name", transaction.AssociatedFileName);

            await _transactionRepository.RemoveAssociatedFileAsync(1);

            transaction = await _transactionRepository.GetByIdAsync(1);

            Assert.IsNotNull(transaction);

            Assert.IsNull(transaction.AssociatedFileName);
        }

        [TestMethod]
        public async Task GetAllExternalIds()
        {
            await Update();

            List<string> externalIds = await _transactionRepository.GetExistingExternalIdsAsync();

            Assert.AreEqual(2, externalIds.Count);

            Assert.IsTrue(externalIds.Contains(TestDataProvider.BokurTransaction1.ExternalId!));
            Assert.IsTrue(externalIds.Contains(TestDataProvider.BokurTransaction2.ExternalId!));
        }

        [TestMethod]
        public async Task SendMockedEmail()
        {
            bool originalValue = GlobalSettings.MocketEnvironment;
            GlobalSettings.MocketEnvironment = true;

            await Create();

            BokurTransaction? transaction = await _transactionRepository.GetByIdAsync(1);

            Assert.IsNotNull(transaction);

            await EmailManager.Instance.SendEmailAsync(transaction.CreateNewTransactionEmail("adam@sakur.se"));

            GlobalSettings.MocketEnvironment = originalValue;
        }

        [TestMethod]
        public async Task GetExternalIdsWhenNullValuesExist()
        {
            await _transactionRepository.CreateAsync(new BokurTransaction(0, "externalid", "Test Non Null", 20, DateTime.Now, null, null, false, null, false, null, false));
            await _transactionRepository.CreateAsync(new BokurTransaction(0, null, "Test With Null", 20, DateTime.Now, null, null, false, null, false, null, false));

            List<string> externalIds = await _transactionRepository.GetExistingExternalIdsAsync();

            Assert.AreEqual(1, externalIds.Count);
        }
    }
}