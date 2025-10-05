using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.UnitOfWork.Interfaces
{
    public interface IVoucherRepository : IRepository<Voucher>
    {
        // Lấy theo ID hoặc Code
        Task<Voucher?> GetByIdAsync(int id);
        Task<Voucher?> GetByCodeAsync(string code);

        // CRUD
        Task AddAsync(Voucher voucher);
        Task UpdateAsync(Voucher voucher);
        Task DeleteAsync(int id);

        // Filter/Search/Pagination
        Task<(IEnumerable<Voucher> Data, int TotalCount)> GetPagedVouchersAsync(
            int pageIndex, int pageSize,
            string? searchKeyword = null,
            string? status = null);
    }
}
