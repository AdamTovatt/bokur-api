using BokurApi.Models.Bokur;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BokurApi.Repositories
{
    public interface IAccountRepository
    {
        Task CreateAsync(string name);
        Task SetName(int id, string name);
        Task<List<BokurAccount>> GetAllAsync();
        Task<BokurAccount?> GetByIdAsync(int id);
    }
} 