using BokurApi;
using BokurApi.Managers.Emails;
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
            await AccountRepository.Instance.CreateAsync("Shared");
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

            transaction = transactions[0];

            transaction.AffectedAccount = await AccountRepository.Instance.GetByIdAsync(2);
            transaction.IgnoreFileRequirement = true;

            await TransactionRepository.Instance.UpdateAsync(transaction);

            transactions = await TransactionRepository.Instance.GetAllThatRequiresActionAsync();

            Assert.AreEqual(0, transactions.Count);
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
            await TransactionRepository.Instance.CreateAsync(TestDataProvider.BokurTransaction1); // take 100
            await TransactionRepository.Instance.CreateAsync(TestDataProvider.BokurTransaction2); // take 21.34
            await TransactionRepository.Instance.CreateAsync(TestDataProvider.BokurTransaction3); // give 20000

            List<AccountSummary> accountSummaries = await TransactionRepository.Instance.GetSummaryAsync();

            Assert.AreEqual(3, accountSummaries.Count);

            foreach(AccountSummary summary in accountSummaries)
                Assert.AreEqual(0, summary.Balance);

            await TransactionRepository.Instance.SetAffectedAccountAsync(1, 1); // assign the take 100 to Adam
            await TransactionRepository.Instance.SetAffectedAccountAsync(2, 2); // assign the take 21.34 to Oliver
            await TransactionRepository.Instance.SetAffectedAccountAsync(3, 3); // assign the give 20000 to Shared

            accountSummaries = await TransactionRepository.Instance.GetSummaryAsync();

            Assert.AreEqual(3, accountSummaries.Count);

            Assert.AreEqual(-100, accountSummaries.TakeByAccount("Adam").Balance); // check that Adam has -100
            Assert.AreEqual(-21.34m, accountSummaries.TakeByAccount("Oliver").Balance); // check that Oliver has -21.34
            Assert.AreEqual(20000, accountSummaries.TakeByAccount("Shared").Balance); // check that Shared has 20000

            await TransactionRepository.Instance.CreateTransferAsync(3, accountSummaries.TakeAccountId("Oliver"), 400); // give 400 to Oliver
            await TransactionRepository.Instance.CreateTransferAsync(3, accountSummaries.TakeAccountId("Adam"), 200); // give 200 to Adam       (both from Shared)
            // the above two lines should mean Shared lost 600

            accountSummaries = await TransactionRepository.Instance.GetSummaryAsync();

            Assert.AreEqual(3, accountSummaries.Count);

            Assert.AreEqual(378.66m, accountSummaries.TakeByAccount("Oliver").Balance); // check that Oliver has -21.34 + 400 = 378.66
            Assert.AreEqual(19400, accountSummaries.TakeByAccount("Shared").Balance); // check that Shared has 20000 - 600 = 19400
            Assert.AreEqual(100, accountSummaries.TakeByAccount("Adam").Balance); // check that Adam has -100 + 200 = 100

            await TransactionRepository.Instance.CreateTransferAsync(3, accountSummaries.TakeAccountId("Oliver"), 576); // give 576 to Oliver from Shared

            accountSummaries = await TransactionRepository.Instance.GetSummaryAsync();
            Assert.AreEqual(3, accountSummaries.Count);

            // Oliver had 378.66, so now he should have 378.66 + 576 = 954.66

            Assert.AreEqual(954.66m, accountSummaries.TakeByAccount("Oliver").Balance); // check that Oliver has 954.66
            Assert.AreEqual(18824, accountSummaries.TakeByAccount("Shared").Balance); // check that Shared has 19400 - 576 = 18824
            Assert.AreEqual(100, accountSummaries.TakeByAccount("Adam").Balance); // check that Adam has 100

            BokurTransaction? transaction = await TransactionRepository.Instance.GetByIdAsync(3);

            Assert.IsNotNull(transaction);
            Assert.IsNotNull(transaction.Children);

            BokurTransaction singleSibling = transaction.Children.Last();

            Assert.AreEqual(576, singleSibling.Value); // find the transaction that added 576 to Oliver

            await TransactionRepository.Instance.DeleteTransactionAsync(singleSibling.Id);

            accountSummaries = await TransactionRepository.Instance.GetSummaryAsync();

            Assert.AreEqual(3, accountSummaries.Count);

            Assert.AreEqual(378.66m, accountSummaries.TakeByAccount("Oliver").Balance);
            Assert.AreEqual(19400, accountSummaries.TakeByAccount("Shared").Balance);
            Assert.AreEqual(100, accountSummaries.TakeByAccount("Adam").Balance);

            BokurTransaction? transaction2 = await TransactionRepository.Instance.GetByIdAsync(3);

            Assert.IsNotNull(transaction2);
            Assert.IsNotNull(transaction2.Children);

            Assert.AreEqual(transaction2.Children.Count, transaction.Children.Count - 2);
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

        [TestMethod]
        public async Task SendMockedEmail()
        {
            bool originalValue = GlobalSettings.MocketEnvironment;
            GlobalSettings.MocketEnvironment = true;

            await Create();

            BokurTransaction? transaction = await TransactionRepository.Instance.GetByIdAsync(1);

            Assert.IsNotNull(transaction);

            await EmailManager.Instance.SendEmailAsync(transaction.CreateNewTransactionEmail("adam@sakur.se"));

            GlobalSettings.MocketEnvironment = originalValue;
        }

        [TestMethod]
        public async Task GetExternalIdsWhenNullValuesExist()
        {
            await TransactionRepository.Instance.CreateAsync(new BokurTransaction(0, "externalid", "Test Non Null", 20, DateTime.Now, null, null, false, null, false, null, false));
            await TransactionRepository.Instance.CreateAsync(new BokurTransaction(0, null, "Test With Null", 20, DateTime.Now, null, null, false, null, false, null, false));

            List<string> externalIds = await TransactionRepository.Instance.GetExistingExternalIdsAsync();

            Assert.AreEqual(1, externalIds.Count);
        }
    }
}