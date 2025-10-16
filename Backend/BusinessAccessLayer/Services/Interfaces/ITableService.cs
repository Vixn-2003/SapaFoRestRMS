using BusinessAccessLayer.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface ITableService
    {
        Task<(IEnumerable<TableManageDto> Tables, int TotalCount)> GetTablesAsync(
            string? search,
            int? capacity,
            int? areaId,
            int page,
            int pageSize, string? status );

        Task<TableManageDto?> GetByIdAsync(int id);
        Task AddAsync(TableManageDto dto);
        Task UpdateAsync(int id, TableManageDto dto);
        Task<(bool Success, string Message)> DeleteAsync(int id);

    }
}
