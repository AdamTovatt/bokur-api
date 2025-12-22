namespace BokurApi.Services
{
    public interface IExportService
    {
        Task ExportTransactionsAsync(DateTime startDate, DateTime endDate, Stream outputStream);
    }
}

