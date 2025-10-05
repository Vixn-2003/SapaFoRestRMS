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
        public VoucherRepository(SapaFoRestRmsContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Voucher>> GetFilteredVouchersAsync(
            string? searchKeyword,
            string? discountType,
            decimal? discountValue,
            DateOnly? startDate,
            DateOnly? endDate,
            decimal? minOrderValue,
            decimal? maxDiscount,
            int pageNumber,
            int pageSize)
        {
            var query = _dbSet.AsQueryable();

            //  Search theo Code + Description
            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                query = query.Where(v =>
                    v.Code.Contains(searchKeyword) ||
                    (v.Description ?? "").Contains(searchKeyword));
            }

            //  Filter theo DiscountType
            if (!string.IsNullOrWhiteSpace(discountType))
            {
                query = query.Where(v => v.DiscountType == discountType);
            }

            //  Filter theo DiscountValue
            if (discountValue.HasValue)
            {
                query = query.Where(v => v.DiscountValue == discountValue);
            }

            //  Filter theo StartDate / EndDate
            if (startDate.HasValue)
            {
                query = query.Where(v => v.StartDate >= startDate);
            }

            if (endDate.HasValue)
            {
                query = query.Where(v => v.EndDate <= endDate);
            }

            //  Filter theo MinOrderValue / MaxDiscount
            if (minOrderValue.HasValue)
            {
                query = query.Where(v => v.MinOrderValue >= minOrderValue);
            }

            if (maxDiscount.HasValue)
            {
                query = query.Where(v => v.MaxDiscount <= maxDiscount);
            }

            //  Phân trang + Sắp xếp
            query = query
                .OrderByDescending(v => v.VoucherId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            return await query.ToListAsync();
        }

        public async Task<int> CountFilteredVouchersAsync(
            string? searchKeyword,
            string? discountType,
            decimal? discountValue,
            DateOnly? startDate,
            DateOnly? endDate,
            decimal? minOrderValue,
            decimal? maxDiscount)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchKeyword))
                query = query.Where(v =>
                    v.Code.Contains(searchKeyword) ||
                    (v.Description ?? "").Contains(searchKeyword));

            if (!string.IsNullOrWhiteSpace(discountType))
                query = query.Where(v => v.DiscountType == discountType);

            if (discountValue.HasValue)
                query = query.Where(v => v.DiscountValue == discountValue);

            if (startDate.HasValue)
                query = query.Where(v => v.StartDate >= startDate);

            if (endDate.HasValue)
                query = query.Where(v => v.EndDate <= endDate);

            if (minOrderValue.HasValue)
                query = query.Where(v => v.MinOrderValue >= minOrderValue);

            if (maxDiscount.HasValue)
                query = query.Where(v => v.MaxDiscount <= maxDiscount);

            return await query.CountAsync();
        }
    }
}
