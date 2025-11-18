using BusinessAccessLayer.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IPayrollService
    {
        Task<IEnumerable<PayrollDTO>> GetAllAsync();
        Task<PayrollDTO?> GetByIdAsync(int id);
        Task AddAsync(PayrollDTO dto);
        Task UpdateAsync(PayrollDTO dto);
        Task DeleteAsync(int id);

        // Search theo StaffName
        Task<(IEnumerable<PayrollDTO> Data, int TotalCount)> SearchFilterSortPagedAsync(
         int pageNumber,
         int pageSize,
         string? staffName = null,
         string? sortBy = null,
         bool descending = false,
         decimal? minBaseSalary = null,
         decimal? maxBaseSalary = null,
         int? minWorkDays = null,
         int? maxWorkDays = null,
         decimal? minBonus = null,
         decimal? maxBonus = null,
         decimal? minPenalty = null,
         decimal? maxPenalty = null,
         decimal? minNetSalary = null,
         decimal? maxNetSalary = null,
         string? monthYear = null);
    }
}
