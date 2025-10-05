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
            // 1. Validate ngày
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                throw new ArgumentException("StartDate phải nhỏ hơn hoặc bằng EndDate.");
            }

            // 2. Validate các giá trị không âm
            if ((discountValue.HasValue && discountValue < 0) ||
                (minOrderValue.HasValue && minOrderValue < 0) ||
                (maxDiscount.HasValue && maxDiscount < 0))
            {
                throw new ArgumentException("Các giá trị số không được phép âm.");
            }

            var query = _dbSet.AsQueryable();

            // 3. Trim keyword và check để tránh case "code     "
            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                var trimmedKeyword = searchKeyword.Trim();
                if (!string.IsNullOrEmpty(trimmedKeyword))
                {
                    query = query.Where(v =>
                        v.Code.Contains(trimmedKeyword) ||
                        (v.Description ?? "").Contains(trimmedKeyword));
                }
            }

            if (!string.IsNullOrWhiteSpace(discountType))
            {
                query = query.Where(v => v.DiscountType == discountType);
            }

            if (discountValue.HasValue)
            {
                query = query.Where(v => v.DiscountValue == discountValue);
            }

            if (startDate.HasValue)
            {
                query = query.Where(v => v.StartDate >= startDate);
            }

            if (endDate.HasValue)
            {
                query = query.Where(v => v.EndDate <= endDate);
            }

            if (minOrderValue.HasValue)
            {
                query = query.Where(v => v.MinOrderValue >= minOrderValue);
            }

            if (maxDiscount.HasValue)
            {
                query = query.Where(v => v.MaxDiscount <= maxDiscount);
            }

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
