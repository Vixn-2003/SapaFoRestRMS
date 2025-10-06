using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IVoucherRepository : IRepository<Voucher>
    {
        /// <summary>
        /// Tìm kiếm + lọc + phân trang voucher theo nhiều tiêu chí.
        /// </summary>
        Task<IEnumerable<Voucher>> GetFilteredVouchersAsync(
            string? searchKeyword,         // search theo Code, Description
            string? discountType,          // lọc theo loại giảm giá
            decimal? discountValue,        // lọc theo giá trị giảm
            DateOnly? startDate,           // lọc theo thời gian bắt đầu
            DateOnly? endDate,             // lọc theo thời gian kết thúc
            decimal? minOrderValue,        // lọc theo giá trị đơn tối thiểu
            decimal? maxDiscount,          // lọc theo giá trị giảm tối đa
            int pageNumber,
            int pageSize);

        /// <summary>
        /// Đếm tổng số voucher thỏa mãn điều kiện (dùng cho phân trang).
        /// </summary>
        Task<int> CountFilteredVouchersAsync(
            string? searchKeyword,
            string? discountType,
            decimal? discountValue,
            DateOnly? startDate,
            DateOnly? endDate,
            decimal? minOrderValue,
            decimal? maxDiscount);

        /// <summary>
        /// Lấy tất cả voucher đã bị xóa mềm (IsDelete = true).
        /// </summary>
        Task<IEnumerable<Voucher>> GetDeletedVouchersAsync(string? searchKeyword,
    string? discountType,
    int pageNumber,
    int pageSize);

        Task<int> CountDeletedVouchersAsync(string? searchKeyword, string? discountType);

    }
}
