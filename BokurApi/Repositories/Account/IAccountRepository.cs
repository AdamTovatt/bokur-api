using BokurApi.Models.Bokur;

namespace BokurApi.Repositories.Account
{
    public interface IAccountRepository
    {
        Task CreateAsync(string name);
        Task SetName(int id, string name);
        Task<List<BokurAccount>> GetAllAsync();
        Task<BokurAccount?> GetByIdAsync(int id);
    }
}