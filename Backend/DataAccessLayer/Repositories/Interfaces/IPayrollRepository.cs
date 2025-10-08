using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IPayrollRepository : IRepository<Payroll>
    {

        // IPayrollRepository.cs
        Task<bool> AnyAsync(Expression<Func<Payroll, bool>> predicate);



        Task<(IEnumerable<Payroll> Data, int TotalCount)> SearchFilterPagedAsync(
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
