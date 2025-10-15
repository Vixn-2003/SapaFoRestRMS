using BusinessAccessLayer.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IAreaService
    {
        Task<(IEnumerable<AreaDto> Areas, int TotalCount)> GetAllAsync(string? searchName, int? floor, int page, int pageSize);
        Task<AreaDto?> GetByIdAsync(int id);
        Task<(bool Success, string Message)> CreateAsync(AreaDto dto);
        Task<(bool Success, string Message)> UpdateAsync(AreaDto dto);
        Task<(bool Success, string Message)> DeleteAsync(int id);
    }
}
