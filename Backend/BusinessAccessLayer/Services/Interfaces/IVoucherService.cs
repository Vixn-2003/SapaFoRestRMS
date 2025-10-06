using BusinessAccessLayer.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IVoucherService
    {
        Task<IEnumerable<VoucherDto>> GetAllAsync();
        Task<VoucherDto?> GetByIdAsync(int id);
        Task<VoucherDto> CreateAsync(VoucherCreateDto dto);
        Task<VoucherDto?> UpdateAsync(int id, VoucherUpdateDto dto);
        Task<bool> DeleteAsync(int id);

        Task<(IEnumerable<VoucherDto> data, int totalCount)> SearchFilterPaginateAsync(
            string? keyword,
            string? discountType,
            decimal? discountValue,
            DateOnly? startDate,
            DateOnly? endDate,
            decimal? minOrderValue,
            decimal? maxDiscount,
            int pageNumber,
            int pageSize);

        /// <summary>
        /// Lấy danh sách voucher đã bị xóa (soft delete) để hỗ trợ phục hồi.
        /// </summary>
        Task<(IEnumerable<VoucherDto> data, int totalCount)> GetDeletedVouchersAsync(
      string? searchKeyword,
      string? discountType,
      int pageNumber,
      int pageSize);

        Task<bool> RestoreAsync(int id);

    }
}
