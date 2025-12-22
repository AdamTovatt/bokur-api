using BokurApi.Models.Bokur;
using BokurApi.Repositories.File;
using BokurApi.Repositories.Transaction;
using System.IO.Compression;
using System.Text;

namespace BokurApi.Services
{
    public class ExportService : IExportService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IFileRepository _fileRepository;

        public ExportService(ITransactionRepository transactionRepository, IFileRepository fileRepository)
        {
            _transactionRepository = transactionRepository;
            _fileRepository = fileRepository;
        }

        private string GetMonthFolderName(DateTime date)
        {
            return $"{date.Month:D2}_{date:MMMM}".ToLowerInvariant();
        }

        private string GetPrefixedFileName(string originalFileName, int sequence, DateTime transactionDate, int maxLength = 200)
        {
            string datePrefix = $"{sequence:D3}_{transactionDate:yyyy-MM-dd}_";
            int availableLength = maxLength - datePrefix.Length;

            string extension = Path.GetExtension(originalFileName);
            int extensionLength = extension.Length;

            // If extension is longer than available space, truncate it
            if (extensionLength > availableLength)
            {
                int maxExtensionLength = Math.Max(1, availableLength - 1); // Leave at least 1 char for base name
                extension = extension.Substring(0, maxExtensionLength);
                extensionLength = extension.Length;
            }

            int baseNameLength = Math.Max(0, availableLength - extensionLength);

            string baseName = Path.GetFileNameWithoutExtension(originalFileName);
            if (baseName.Length > baseNameLength)
                baseName = baseName.Substring(0, baseNameLength);

            return $"{datePrefix}{baseName}{extension}";
        }

        public string GetExportFilePath(ExportTransaction transaction, int sequence)
        {
            if (string.IsNullOrEmpty(transaction.FileName))
                return string.Empty;

            string monthFolder = GetMonthFolderName(transaction.TimeOfTransaction);
            string prefixedFileName = GetPrefixedFileName(transaction.FileName, sequence, transaction.TimeOfTransaction);
            return $"files/{monthFolder}/{prefixedFileName}";
        }

        public async Task ExportTransactionsAsync(DateTime startDate, DateTime endDate, Stream outputStream)
        {
            string yearFolder = $"bokur_{startDate:yyyy}";

            List<ExportTransaction> transactions =
                (await _transactionRepository.GetTransactionsForExport(startDate, endDate))
                .Select(ExportTransaction.FromBokurTransaction).ToList();

            // Build mapping of transactions to their export file paths for Excel hyperlinks
            Dictionary<int, string> transactionIdToExportPath = new Dictionary<int, string>();
            List<ExportTransaction> transactionsWithFiles = transactions
                .Where(t => !string.IsNullOrEmpty(t.FileName))
                .OrderBy(t => t.TimeOfTransaction)
                .ToList();

            if (transactionsWithFiles.Any())
            {
                IEnumerable<IGrouping<(int Year, int Month), ExportTransaction>> transactionsByMonth = transactionsWithFiles
                    .GroupBy(t => (t.TimeOfTransaction.Year, t.TimeOfTransaction.Month))
                    .OrderBy(g => g.Key.Year)
                    .ThenBy(g => g.Key.Month);

                foreach (IGrouping<(int Year, int Month), ExportTransaction> monthGroup in transactionsByMonth)
                {
                    int sequence = 1;

                    foreach (ExportTransaction transaction in monthGroup.OrderBy(t => t.TimeOfTransaction))
                    {
                        string exportPath = GetExportFilePath(transaction, sequence);
                        transactionIdToExportPath[transaction.Id] = exportPath;
                        sequence++;
                    }
                }
            }

            // Use outputStream to stream directly to the client
            using (ZipArchive zipStream = new ZipArchive(outputStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                // 1. Create the CSV file entry inside the year folder
                ZipArchiveEntry csvEntry = zipStream.CreateEntry($"{yearFolder}/transactions.csv", CompressionLevel.Fastest);
                using (Stream entryStream = csvEntry.Open())
                using (StreamWriter writer = new StreamWriter(entryStream, Encoding.UTF8))
                {
                    // Write CSV header
                    await ExportTransaction.WriteHeaderAsync(writer);

                    // Write each transaction
                    foreach (ExportTransaction transaction in transactions)
                        await transaction.WriteToCsvAsync(writer);
                }

                // 2. Create the Excel file entry inside the year folder
                ZipArchiveEntry excelEntry = zipStream.CreateEntry($"{yearFolder}/transactions.xlsx", CompressionLevel.Fastest);
                using (Stream excelStream = excelEntry.Open())
                {
                    ExportTransaction.WriteExcelAsync(transactions, excelStream, transactionIdToExportPath);
                }

                // 3. Add each file to month-based folders inside the "files" folder in the ZIP
                if (transactionsWithFiles.Any())
                {
                    IEnumerable<IGrouping<(int Year, int Month), ExportTransaction>> transactionsByMonth = transactionsWithFiles
                        .GroupBy(t => (t.TimeOfTransaction.Year, t.TimeOfTransaction.Month))
                        .OrderBy(g => g.Key.Year)
                        .ThenBy(g => g.Key.Month);

                    foreach (IGrouping<(int Year, int Month), ExportTransaction> monthGroup in transactionsByMonth)
                    {
                        string monthFolder = GetMonthFolderName(new DateTime(monthGroup.Key.Year, monthGroup.Key.Month, 1));
                        int sequence = 1;

                        foreach (ExportTransaction transaction in monthGroup.OrderBy(t => t.TimeOfTransaction))
                        {
                            string? fileName = transaction.FileName;

                            if (!string.IsNullOrEmpty(fileName))
                            {
                                byte[]? fileBytes = await _fileRepository.ReadFileAsync(fileName);

                                if (fileBytes != null)
                                {
                                    string prefixedFileName = GetPrefixedFileName(fileName, sequence, transaction.TimeOfTransaction);
                                    ZipArchiveEntry fileEntry = zipStream.CreateEntry($"{yearFolder}/files/{monthFolder}/{prefixedFileName}", CompressionLevel.Fastest);
                                    using (Stream entryStream = fileEntry.Open())
                                    {
                                        await entryStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                                    }
                                }
                            }

                            sequence++;
                        }
                    }
                }
            }
        }
    }
}

