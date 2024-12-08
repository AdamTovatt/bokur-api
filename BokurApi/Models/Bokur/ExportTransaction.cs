using ClosedXML.Excel;

namespace BokurApi.Models.Bokur
{
    public class ExportTransaction
    {
        public int Id { get; set; }
        public string? ExternalId { get; set; }
        public DateTime TimeOfTransaction { get; set; }
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public string AccountName { get; set; }
        public string? FileName { get; set; }

        public ExportTransaction(int id, string? externalId, DateTime timeOfTransaction, string name, decimal amount, string accountName, string? fileName)
        {
            Id = id;
            ExternalId = externalId;
            TimeOfTransaction = timeOfTransaction;
            Name = name;
            Amount = amount;
            AccountName = accountName;
            FileName = fileName;
        }

        public static async Task WriteHeaderAsync(StreamWriter writer)
        {
            await writer.WriteLineAsync("Id,ExternalId,TimeOfTransaction,Name,Amount,AccountName,FileName");
        }

        public async Task WriteToCsvAsync(StreamWriter writer)
        {
            await writer.WriteLineAsync($"{Id}," +
                                         $"{ExternalId ?? "(missing)"}," +
                                         $"{TimeOfTransaction:O}," +
                                         $"{Name}," +
                                         $"{Amount}," +
                                         $"{AccountName}," +
                                         $"{FileName ?? "(missing file)"}");
        }

        public static void WriteExcelAsync(List<ExportTransaction> transactions, Stream excelStream)
        {
            using (XLWorkbook workbook = new XLWorkbook())
            {
                IXLWorksheet worksheet = workbook.Worksheets.Add("Transactions");

                // Write Excel header
                worksheet.Cell(1, 1).Value = "Id";
                worksheet.Cell(1, 2).Value = "ExternalId";
                worksheet.Cell(1, 3).Value = "TimeOfTransaction";
                worksheet.Cell(1, 4).Value = "Name";
                worksheet.Cell(1, 5).Value = "Amount";
                worksheet.Cell(1, 6).Value = "AccountName";
                worksheet.Cell(1, 7).Value = "File";

                // Apply bold formatting to header
                worksheet.Range("A1:G1").Style.Font.Bold = true;

                // Write each transaction
                int row = 2;
                foreach (ExportTransaction transaction in transactions)
                {
                    worksheet.Cell(row, 1).Value = transaction.Id;
                    worksheet.Cell(row, 2).Value = transaction.ExternalId;
                    worksheet.Cell(row, 3).Value = transaction.TimeOfTransaction;
                    worksheet.Cell(row, 4).Value = transaction.Name;
                    worksheet.Cell(row, 5).Value = transaction.Amount;
                    worksheet.Cell(row, 6).Value = transaction.AccountName;

                    if (!string.IsNullOrEmpty(transaction.FileName))
                    {
                        string filePath = $"files/{transaction.FileName.Trim()}";
                        Uri fileUri = new Uri(filePath, UriKind.Relative);
                        worksheet.Cell(row, 7).SetHyperlink(new XLHyperlink(fileUri));
                        worksheet.Cell(row, 7).Value = transaction.FileName;
                    }

                    // Apply background color based on the Amount value
                    if (transaction.Amount > 0)
                    {
                        worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightGreen;
                    }
                    else if (transaction.Amount < 0)
                    {
                        worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightSalmon;
                    }

                    row++;
                }

                // Auto-adjust column widths
                worksheet.Columns().AdjustToContents();

                // Save the workbook to the stream
                workbook.SaveAs(excelStream);
            }
        }

        public static ExportTransaction FromBokurTransaction(BokurTransaction transaction)
        {
            return new ExportTransaction(
                transaction.Id,
                transaction.ExternalId,
                transaction.Date,
                transaction.Name,
                transaction.Value,
                transaction.AffectedAccount?.Name ?? "???",
                transaction.AssociatedFileName);
        }
    }
}
