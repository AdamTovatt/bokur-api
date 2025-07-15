using BokurApi.Models;
using BokurApi.Models.Bokur;
using BokurApi.Repositories.File;
using BokurApi.Repositories.Transaction; // Ensure you have ClosedXML installed via NuGet
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Text;

namespace BokurApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ExportController : ControllerBase
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IFileRepository _fileRepository;

        public ExportController(ITransactionRepository transactionRepository, IFileRepository fileRepository)
        {
            _transactionRepository = transactionRepository;
            _fileRepository = fileRepository;
        }

        [Authorize(AuthorizationRole.Admin)]
        [HttpGet("exported")]
        public async Task<IActionResult> GetExportedData(DateTime startDate, DateTime? endDate = null)
        {
            DateTime endDateToUse = endDate ?? DateTime.Now;
            string yearFolder = $"bokur_{startDate:yyyy}";

            List<ExportTransaction> transactions =
                (await _transactionRepository.GetTransactionsForExport(startDate, endDateToUse))
                .Select(ExportTransaction.FromBokurTransaction).ToList();

            Response.Headers.Append("Content-Disposition", "attachment; filename=bokur_export.zip");
            Response.ContentType = "application/zip";

            // Use Response.Body to stream directly to the client
            using (ZipArchive zipStream = new ZipArchive(Response.BodyWriter.AsStream(), ZipArchiveMode.Create, leaveOpen: true))
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
                    ExportTransaction.WriteExcelAsync(transactions, excelStream);
                }

                // 3. Add each image to the "files" folder inside the year folder in the ZIP
                foreach (ExportTransaction transaction in transactions)
                {
                    string? fileName = transaction.FileName;

                    if (!string.IsNullOrEmpty(fileName))
                    {
                        byte[]? fileBytes = await _fileRepository.ReadFileAsync(fileName);

                        if (fileBytes != null)
                        {
                            ZipArchiveEntry fileEntry = zipStream.CreateEntry($"{yearFolder}/files/{fileName}", CompressionLevel.Fastest);
                            using (Stream entryStream = fileEntry.Open())
                            {
                                await entryStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                            }
                        }
                    }
                }
            }

            return new EmptyResult(); // Response is already streamed
        }
    }
}
