using BokurApi.Models.Bokur;
using BokurApi.Repositories.Account;
using BokurApi.Helpers.DatabaseConnection;
using BokurApiTests.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BokurApiTests.RepositoryTests
{
    [TestClass]
    public class AccountRepositoryTests
    {
        private static IAccountRepository _accountRepository = null!;

        [ClassInitialize]
        public static void BeforeAll(TestContext testContext)
        {
            string connectionString = BokurApi.Helpers.EnvironmentHelper.GetConnectionString();
            _accountRepository = new AccountRepository(new PostgresConnectionFactory(connectionString));
        }

        [ClassCleanup]
        public static async Task AfterAll()
        {
            await DatabaseHelper.CleanTable("bokur_account");
        }

        [TestInitialize]
        public async Task BeforeEach()
        {
            await DatabaseHelper.CleanTable("bokur_account");
        }

        [TestMethod]
        public async Task CreateAndGetAllAsync()
        {
            await _accountRepository.CreateAsync("Adam");
            await _accountRepository.CreateAsync("Oliver");

            List<BokurAccount> accounts = await _accountRepository.GetAllAsync();

            Assert.AreEqual(2, accounts.Count);
            Assert.IsTrue(accounts.Exists(x => x.Name == "Adam"));
            Assert.IsTrue(accounts.Exists(x => x.Name == "Oliver"));
        }

        [TestMethod]
        public async Task SetName_UpdatesAccountName()
        {
            await _accountRepository.CreateAsync("Adam");
            List<BokurAccount> accounts = await _accountRepository.GetAllAsync();
            int id = accounts[0].Id;

            await _accountRepository.SetName(id, "Eve");
            List<BokurAccount> updatedAccounts = await _accountRepository.GetAllAsync();

            Assert.AreEqual("Eve", updatedAccounts[0].Name);
        }

        [TestMethod]
        public async Task GetByIdAsync_ReturnsCorrectAccount()
        {
            await _accountRepository.CreateAsync("Adam");
            List<BokurAccount> accounts = await _accountRepository.GetAllAsync();
            int id = accounts[0].Id;

            BokurAccount? account = await _accountRepository.GetByIdAsync(id);

            Assert.IsNotNull(account);
            Assert.AreEqual("Adam", account.Name);
        }

        [TestMethod]
        public async Task GetByIdAsync_ReturnsNullForMissingId()
        {
            BokurAccount? account = await _accountRepository.GetByIdAsync(9999);

            Assert.IsNull(account);
        }

        [TestMethod]
        public async Task CacheBehavior_IsResetOnCreateAndSetName()
        {
            await _accountRepository.CreateAsync("Adam");
            List<BokurAccount> accounts = await _accountRepository.GetAllAsync();
            int id = accounts[0].Id;

            await _accountRepository.SetName(id, "Eve");
            List<BokurAccount> updatedAccounts = await _accountRepository.GetAllAsync();

            Assert.AreEqual("Eve", updatedAccounts[0].Name);
        }
    }
}