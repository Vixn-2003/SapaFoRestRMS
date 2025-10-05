using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;

namespace BusinessAccessLayer.Services
{
    public class VoucherService : IVoucherService
    {
        private readonly IVoucherRepository _voucherRepository;

        public VoucherService(IVoucherRepository voucherRepository)
        {
            _voucherRepository = voucherRepository;
        }

        public async Task<IEnumerable<VoucherDto>> GetAllAsync()
        {
            var vouchers = await _voucherRepository.GetAllAsync();
            return vouchers.Select(MapToDto);
        }

        public async Task<VoucherDto?> GetByIdAsync(int id)
        {
            var voucher = await _voucherRepository.GetByIdAsync(id);
            return voucher == null ? null : MapToDto(voucher);
        }

        public async Task<VoucherDto> CreateAsync(VoucherCreateDto dto)
        {
            var voucher = new Voucher
            {
                Code = dto.Code,
                Description = dto.Description,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                MinOrderValue = dto.MinOrderValue,
                MaxDiscount = dto.MaxDiscount,
                Status = dto.Status
            };

            await _voucherRepository.AddAsync(voucher);
            await _voucherRepository.SaveChangesAsync();

            return MapToDto(voucher);
        }

        public async Task<VoucherDto?> UpdateAsync(int id, VoucherUpdateDto dto)
        {
            var voucher = await _voucherRepository.GetByIdAsync(id);
            if (voucher == null) return null;

            voucher.Description = dto.Description;
            voucher.DiscountType = dto.DiscountType;
            voucher.DiscountValue = dto.DiscountValue;
            voucher.StartDate = dto.StartDate;
            voucher.EndDate = dto.EndDate;
            voucher.MinOrderValue = dto.MinOrderValue;
            voucher.MaxDiscount = dto.MaxDiscount;
            voucher.Status = dto.Status;

            await _voucherRepository.UpdateAsync(voucher);
            await _voucherRepository.SaveChangesAsync();

            return MapToDto(voucher);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var voucher = await _voucherRepository.GetByIdAsync(id);
            if (voucher == null) return false;

            voucher.Status = "1"; // Đánh dấu là đã xóa
            _voucherRepository.UpdateAsync(voucher);
            await _voucherRepository.SaveChangesAsync();

            return true;
        }


        public async Task<(IEnumerable<VoucherDto> data, int totalCount)> SearchFilterPaginateAsync(
            string? keyword,
            string? discountType,
            decimal? discountValue,
            DateOnly? startDate,
            DateOnly? endDate,
            decimal? minOrderValue,
            decimal? maxDiscount,
            int pageNumber,
            int pageSize)
        {
            var data = await _voucherRepository.GetFilteredVouchersAsync(
                keyword, discountType, discountValue, startDate, endDate, minOrderValue, maxDiscount, pageNumber, pageSize);

            var count = await _voucherRepository.CountFilteredVouchersAsync(
                keyword, discountType, discountValue, startDate, endDate, minOrderValue, maxDiscount);

            return (data.Select(MapToDto), count);
        }

        private VoucherDto MapToDto(Voucher voucher)
        {
            return new VoucherDto
            {
                VoucherId = voucher.VoucherId,
                Code = voucher.Code,
                Description = voucher.Description,
                DiscountType = voucher.DiscountType,
                DiscountValue = voucher.DiscountValue,
                StartDate = voucher.StartDate,
                EndDate = voucher.EndDate,
                MinOrderValue = voucher.MinOrderValue,
                MaxDiscount = voucher.MaxDiscount,
                Status = voucher.Status
            };
        }
    }
}
