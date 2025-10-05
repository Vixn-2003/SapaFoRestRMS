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
    public class VoucherRepository : Repository<Voucher>, IVoucherRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public VoucherRepository(SapaFoRestRmsContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Voucher?> GetByIdAsync(int id)
        {
            return await _context.Vouchers.FindAsync(id);
        }

        public async Task<Voucher?> GetByCodeAsync(string code)
        {
            return await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == code);
        }

        public async Task AddAsync(Voucher voucher)
        {
            await _context.Vouchers.AddAsync(voucher);
        }

        public async Task UpdateAsync(Voucher voucher)
        {
            _context.Vouchers.Update(voucher);
        }

        public async Task DeleteAsync(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher != null)
            {
                _context.Vouchers.Remove(voucher);
            }
        }

        public async Task<(IEnumerable<Voucher> Data, int TotalCount)> GetPagedVouchersAsync(
            int pageIndex, int pageSize,
            string? searchKeyword = null,
            string? status = null)
        {
            var query = _context.Vouchers.AsQueryable();

            // Filter theo status
            if (!string.IsNullOrEmpty(status))
                query = query.Where(v => v.Status == status);

            // Search theo Code hoặc Description
            if (!string.IsNullOrEmpty(searchKeyword))
            {
                query = query.Where(v =>
                    v.Code.Contains(searchKeyword) ||
                    (v.Description != null && v.Description.Contains(searchKeyword)));
            }

            var totalCount = await query.CountAsync();

            var data = await query
                .OrderByDescending(v => v.StartDate)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, totalCount);
        }
    }
}
