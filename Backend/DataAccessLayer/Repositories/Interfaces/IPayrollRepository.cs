using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IPayrollRepository : IRepository<Payroll>
    {
        Task<IEnumerable<Payroll>> SearchAsync(string? staffName);
        Task<IEnumerable<Payroll>> FilterAsync(
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
        Task<(IEnumerable<Payroll> Data, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? staffName,
            string? sortBy,
            bool descending);
    }
}
