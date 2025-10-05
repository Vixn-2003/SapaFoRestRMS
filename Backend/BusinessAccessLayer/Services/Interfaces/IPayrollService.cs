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
        Task<IEnumerable<PayrollDTO>> SearchAsync(string staffName);

        // Filter nâng cao
        Task<IEnumerable<PayrollDTO>> FilterAsync(
            string? sortBy,
            bool descending,
            decimal? minBaseSalary,
            decimal? maxBaseSalary,
            int? minWorkDays,
            int? maxWorkDays,
            decimal? minBonus,
            decimal? maxBonus,
            decimal? minPenalty,
            decimal? maxPenalty,
            decimal? minNetSalary,
            decimal? maxNetSalary,
            string? monthYear);

        // Phân trang
        Task<(IEnumerable<PayrollDTO> Data, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? staffName,
            string? sortBy,
            bool descending);
    }
}
