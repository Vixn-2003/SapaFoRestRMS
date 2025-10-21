using DomainAccessLayer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface ITableRepository
    {
        Task<IEnumerable<Table>> GetAllAsync(string? search, int? capacity, int? areaId, int page, int pageSize, string? status = null);
        Task<int> GetCountAsync(string? search, int? capacity, int? areaId);
        Task<Table?> GetByIdAsync(int id);
        Task AddAsync(Table table);
        Task UpdateAsync(Table table);
        Task DeleteAsync(int id);
        Task<bool> IsTableInUseAsync(int tableId);
        Task SaveAsync();
    }
}
