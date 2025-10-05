using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class PayrollRepository : Repository<Payroll>, IPayrollRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public PayrollRepository(SapaFoRestRmsContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Payroll>> SearchAsync(string? staffName)
        {
            var query = _context.Payrolls
                .Include(p => p.Staff).Include(p=>p.Staff).ThenInclude(p=>p.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(staffName))
            {
                query = query.Where(p => p.Staff.User.FullName.Contains(staffName));
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Payroll>> FilterAsync(
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
            string? monthYear)
        {
            var query = _context.Payrolls.Include(p => p.Staff).AsQueryable();

            if (minBaseSalary.HasValue)
                query = query.Where(p => p.BaseSalary >= minBaseSalary.Value);
            if (maxBaseSalary.HasValue)
                query = query.Where(p => p.BaseSalary <= maxBaseSalary.Value);

            if (minWorkDays.HasValue)
                query = query.Where(p => p.TotalWorkDays >= minWorkDays.Value);
            if (maxWorkDays.HasValue)
                query = query.Where(p => p.TotalWorkDays <= maxWorkDays.Value);

            if (minBonus.HasValue)
                query = query.Where(p => p.TotalBonus >= minBonus.Value);
            if (maxBonus.HasValue)
                query = query.Where(p => p.TotalBonus <= maxBonus.Value);

            if (minPenalty.HasValue)
                query = query.Where(p => p.TotalPenalty >= minPenalty.Value);
            if (maxPenalty.HasValue)
                query = query.Where(p => p.TotalPenalty <= maxPenalty.Value);

            if (minNetSalary.HasValue)
                query = query.Where(p => p.NetSalary >= minNetSalary.Value);
            if (maxNetSalary.HasValue)
                query = query.Where(p => p.NetSalary <= maxNetSalary.Value);

            if (!string.IsNullOrWhiteSpace(monthYear))
                query = query.Where(p => p.MonthYear.Contains(monthYear));

            // Sắp xếp
            query = sortBy?.ToLower() switch
            {
                "basesalary" => descending ? query.OrderByDescending(p => p.BaseSalary) : query.OrderBy(p => p.BaseSalary),
                "monthyear" => descending ? query.OrderByDescending(p => p.MonthYear) : query.OrderBy(p => p.MonthYear),
                "totalworkdays" => descending ? query.OrderByDescending(p => p.TotalWorkDays) : query.OrderBy(p => p.TotalWorkDays),
                "totalbonus" => descending ? query.OrderByDescending(p => p.TotalBonus) : query.OrderBy(p => p.TotalBonus),
                "totalpenalty" => descending ? query.OrderByDescending(p => p.TotalPenalty) : query.OrderBy(p => p.TotalPenalty),
                "netsalary" => descending ? query.OrderByDescending(p => p.NetSalary) : query.OrderBy(p => p.NetSalary),
                _ => query.OrderBy(p => p.PayrollId)
            };

            return await query.ToListAsync();
        }

        public async Task<(IEnumerable<Payroll> Data, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? staffName,
            string? sortBy,
            bool descending)
        {
            var query = _context.Payrolls.Include(p => p.Staff).Include(p => p.Staff).
                ThenInclude(p => p.User).AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(staffName))
            {
                query = query.Where(p => p.Staff.User.FullName.Contains(staffName));
            }

            // Sort
            query = sortBy?.ToLower() switch
            {
                "basesalary" => descending ? query.OrderByDescending(p => p.BaseSalary) : query.OrderBy(p => p.BaseSalary),
                "monthyear" => descending ? query.OrderByDescending(p => p.MonthYear) : query.OrderBy(p => p.MonthYear),
                "totalworkdays" => descending ? query.OrderByDescending(p => p.TotalWorkDays) : query.OrderBy(p => p.TotalWorkDays),
                "totalbonus" => descending ? query.OrderByDescending(p => p.TotalBonus) : query.OrderBy(p => p.TotalBonus),
                "totalpenalty" => descending ? query.OrderByDescending(p => p.TotalPenalty) : query.OrderBy(p => p.TotalPenalty),
                "netsalary" => descending ? query.OrderByDescending(p => p.NetSalary) : query.OrderBy(p => p.NetSalary),
                _ => query.OrderBy(p => p.PayrollId)
            };

            var totalCount = await query.CountAsync();

            var data = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return (data, totalCount);
        }
    }
}
