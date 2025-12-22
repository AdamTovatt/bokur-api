using BokurApi.Models.Bokur;
using BokurApi.Repositories.File;
using BokurApi.Repositories.Transaction;
using BokurApi.Services;
using BokurApiTests.InMemoryRepositories;
using BokurApiTests.TestUtilities;
using System.IO.Compression;
using System.Text;

namespace BokurApiTests.ServiceTests
{
    [TestClass]
    public class ExportServiceTests
    {
        private IExportService _exportService = null!;
        private InMemoryTransactionRepository _transactionRepository = null!;
        private InMemoryFileRepository _fileRepository = null!;
        private InMemoryAccountRepository _accountRepository = null!;

        [TestInitialize]
        public void Setup()
        {
            _accountRepository = new InMemoryAccountRepository();
            _transactionRepository = new InMemoryTransactionRepository();
            _fileRepository = new InMemoryFileRepository();
            _exportService = new ExportService(_transactionRepository, _fileRepository);
        }

        [TestMethod]
        public async Task ExportTransactions_WithTransactionsAndFiles_CreatesValidZip()
        {
            // Arrange
            await _accountRepository.CreateAsync("TestAccount");
            BokurAccount account = (await _accountRepository.GetAllAsync()).First();

            DateTime startDate = new DateTime(2024, 1, 1);
            DateTime endDate = new DateTime(2024, 12, 31);

            BokurTransaction transaction1 = new BokurTransaction(
                0,
                "ext1",
                "Transaction 1",
                100.50m,
                new DateTime(2024, 6, 15),
                "file1.jpg",
                account,
                false,
                null,
                false,
                null,
                false);

            BokurTransaction transaction2 = new BokurTransaction(
                0,
                "ext2",
                "Transaction 2",
                -50.25m,
                new DateTime(2024, 7, 20),
                "file2.png",
                account,
                false,
                null,
                false,
                null,
                false);

            int id1 = await _transactionRepository.CreateAsync(transaction1);
            int id2 = await _transactionRepository.CreateAsync(transaction2);

            byte[] file1Bytes = Encoding.UTF8.GetBytes("File 1 content");
            byte[] file2Bytes = Encoding.UTF8.GetBytes("File 2 content");
            await _fileRepository.SaveFileAsync("file1.jpg", file1Bytes);
            await _fileRepository.SaveFileAsync("file2.png", file2Bytes);

            // Act
            using (MemoryStream outputStream = new MemoryStream())
            {
                await _exportService.ExportTransactionsAsync(startDate, endDate, outputStream);

                // Assert
                outputStream.Position = 0;
                using (ZipArchive zip = new ZipArchive(outputStream, ZipArchiveMode.Read))
                {
                    // Verify ZIP structure
                    Assert.IsTrue(zip.Entries.Any(e => e.FullName == "bokur_2024/transactions.csv"));
                    Assert.IsTrue(zip.Entries.Any(e => e.FullName == "bokur_2024/transactions.xlsx"));
                    Assert.IsTrue(zip.Entries.Any(e => e.FullName == "bokur_2024/files/file1.jpg"));
                    Assert.IsTrue(zip.Entries.Any(e => e.FullName == "bokur_2024/files/file2.png"));

                    // Verify CSV content
                    ZipArchiveEntry csvEntry = zip.GetEntry("bokur_2024/transactions.csv")!;
                    using (Stream csvStream = csvEntry.Open())
                    using (StreamReader reader = new StreamReader(csvStream, Encoding.UTF8))
                    {
                        string csvContent = await reader.ReadToEndAsync();
                        Assert.IsTrue(csvContent.Contains("Id,ExternalId,TimeOfTransaction,Name,Amount,AccountName,FileName"));
                        Assert.IsTrue(csvContent.Contains("Transaction 1"));
                        Assert.IsTrue(csvContent.Contains("Transaction 2"));
                        // Check for amount with either comma or period as decimal separator (locale-dependent)
                        Assert.IsTrue(csvContent.Contains("100,50") || csvContent.Contains("100.50") || csvContent.Contains("100.5"), $"CSV content should contain 100.50. Actual content: {csvContent}");
                        Assert.IsTrue(csvContent.Contains("-50,25") || csvContent.Contains("-50.25"), $"CSV content should contain -50.25. Actual content: {csvContent}");
                    }

                    // Verify file content
                    ZipArchiveEntry file1Entry = zip.GetEntry("bokur_2024/files/file1.jpg")!;
                    using (Stream fileStream = file1Entry.Open())
                    {
                        byte[] extractedBytes = new byte[file1Entry.Length];
                        await fileStream.ReadAsync(extractedBytes, 0, extractedBytes.Length);
                        CollectionAssert.AreEqual(file1Bytes, extractedBytes);
                    }
                }
            }
        }

        [TestMethod]
        public async Task ExportTransactions_WithTransactionsButNoFiles_CreatesZipWithoutFiles()
        {
            // Arrange
            await _accountRepository.CreateAsync("TestAccount");
            BokurAccount account = (await _accountRepository.GetAllAsync()).First();

            DateTime startDate = new DateTime(2024, 1, 1);
            DateTime endDate = new DateTime(2024, 12, 31);

            BokurTransaction transaction = new BokurTransaction(
                0,
                "ext1",
                "Transaction without file",
                100m,
                new DateTime(2024, 6, 15),
                null,
                account,
                false,
                null,
                false,
                null,
                false);

            await _transactionRepository.CreateAsync(transaction);

            // Act
            using (MemoryStream outputStream = new MemoryStream())
            {
                await _exportService.ExportTransactionsAsync(startDate, endDate, outputStream);

                // Assert
                outputStream.Position = 0;
                using (ZipArchive zip = new ZipArchive(outputStream, ZipArchiveMode.Read))
                {
                    Assert.IsTrue(zip.Entries.Any(e => e.FullName == "bokur_2024/transactions.csv"));
                    Assert.IsTrue(zip.Entries.Any(e => e.FullName == "bokur_2024/transactions.xlsx"));
                    Assert.IsFalse(zip.Entries.Any(e => e.FullName.StartsWith("bokur_2024/files/")));
                }
            }
        }

        [TestMethod]
        public async Task ExportTransactions_WithEmptyTransactionList_CreatesZipWithEmptyFiles()
        {
            // Arrange
            DateTime startDate = new DateTime(2024, 1, 1);
            DateTime endDate = new DateTime(2024, 12, 31);

            // Act
            using (MemoryStream outputStream = new MemoryStream())
            {
                await _exportService.ExportTransactionsAsync(startDate, endDate, outputStream);

                // Assert
                outputStream.Position = 0;
                using (ZipArchive zip = new ZipArchive(outputStream, ZipArchiveMode.Read))
                {
                    Assert.IsTrue(zip.Entries.Any(e => e.FullName == "bokur_2024/transactions.csv"));
                    Assert.IsTrue(zip.Entries.Any(e => e.FullName == "bokur_2024/transactions.xlsx"));

                    // Verify CSV has header but no data rows
                    ZipArchiveEntry csvEntry = zip.GetEntry("bokur_2024/transactions.csv")!;
                    using (Stream csvStream = csvEntry.Open())
                    using (StreamReader reader = new StreamReader(csvStream, Encoding.UTF8))
                    {
                        string csvContent = await reader.ReadToEndAsync();
                        Assert.IsTrue(csvContent.Contains("Id,ExternalId,TimeOfTransaction,Name,Amount,AccountName,FileName"));
                        string[] lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        Assert.AreEqual(1, lines.Length); // Only header line
                    }
                }
            }
        }

        [TestMethod]
        public async Task ExportTransactions_WithMissingFile_SkipsMissingFile()
        {
            // Arrange
            await _accountRepository.CreateAsync("TestAccount");
            BokurAccount account = (await _accountRepository.GetAllAsync()).First();

            DateTime startDate = new DateTime(2024, 1, 1);
            DateTime endDate = new DateTime(2024, 12, 31);

            BokurTransaction transaction = new BokurTransaction(
                0,
                "ext1",
                "Transaction with missing file",
                100m,
                new DateTime(2024, 6, 15),
                "missing.jpg",
                account,
                false,
                null,
                false,
                null,
                false);

            await _transactionRepository.CreateAsync(transaction);
            // Don't save the file to repository

            // Act
            using (MemoryStream outputStream = new MemoryStream())
            {
                await _exportService.ExportTransactionsAsync(startDate, endDate, outputStream);

                // Assert
                outputStream.Position = 0;
                using (ZipArchive zip = new ZipArchive(outputStream, ZipArchiveMode.Read))
                {
                    Assert.IsTrue(zip.Entries.Any(e => e.FullName == "bokur_2024/transactions.csv"));
                    Assert.IsFalse(zip.Entries.Any(e => e.FullName == "bokur_2024/files/missing.jpg"));
                }
            }
        }

        [TestMethod]
        public async Task ExportTransactions_UsesCorrectYearFolder()
        {
            // Arrange
            DateTime startDate = new DateTime(2023, 1, 1);
            DateTime endDate = new DateTime(2023, 12, 31);

            // Act
            using (MemoryStream outputStream = new MemoryStream())
            {
                await _exportService.ExportTransactionsAsync(startDate, endDate, outputStream);

                // Assert
                outputStream.Position = 0;
                using (ZipArchive zip = new ZipArchive(outputStream, ZipArchiveMode.Read))
                {
                    Assert.IsTrue(zip.Entries.Any(e => e.FullName == "bokur_2023/transactions.csv"));
                    Assert.IsTrue(zip.Entries.Any(e => e.FullName == "bokur_2023/transactions.xlsx"));
                }
            }
        }
    }
}

