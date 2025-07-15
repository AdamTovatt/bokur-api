using BokurApi.Models.Bokur;

namespace BokurApi.Repositories.Transaction
{
    public interface ITransactionRepository
    {
        Task<int> CreateAsync(BokurTransaction transaction);
        Task UpdateAsync(BokurTransaction transaction);
        Task RemoveAssociatedFileAsync(int transactionId);
        Task SetAffectedAccountAsync(int transactionId, int accountId);
        Task<bool> CreateTransferAsync(int parentTransactionId, int toAccountId, decimal amount);
        Task SetTransactionValueAsync(int transactionId, decimal newValue);
        Task DeleteTransactionAsync(int transactionId);
        Task<BokurTransaction?> GetByIdAsync(int id);
        Task<List<BokurTransaction>> GetAllThatRequiresActionAsync();
        Task<List<BokurTransaction>> GetAllChildrenForParentAsync(Npgsql.NpgsqlConnection connection, int parentId);
        Task<List<BokurTransaction>> GetAllForMonthAsync(DateTime month);
        Task<List<BokurTransaction>> GetTransactionsForExport(DateTime startDate, DateTime endDate);
        Task<List<BokurTransaction>> GetAllWithoutParentAsync(int pageSize = 10, int page = 0, int? accountId = null);
        Task<List<string>> GetExistingExternalIdsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<AccountSummary>> GetSummaryAsync();
    }
}