using DomainAccessLayer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IAreaRepository
    {
        Task<IEnumerable<Area>> GetAllAsync(string? searchName, int? floor, int page, int pageSize);
        Task<int> GetTotalCountAsync(string? searchName, int? floor);
        Task<Area?> GetByIdAsync(int id);
        Task AddAsync(Area area);
        Task UpdateAsync(Area area);
        Task DeleteAsync(Area area);
        Task<bool> ExistsAsync(string areaName, int floor, int? excludeId = null);
        Task<bool> HasTablesAsync(int areaId);
    }
}
