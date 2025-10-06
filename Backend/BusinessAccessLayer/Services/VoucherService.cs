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
            // 1. Mã voucher không được để trống
            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new Exception("Mã voucher không được để trống.");

            // Chuyển DateTime sang DateOnly để so sánh
            var startDate = DateOnly.FromDateTime(dto.StartDate);
            var endDate = DateOnly.FromDateTime(dto.EndDate);
            var today = DateOnly.FromDateTime(DateTime.Today);

            // 2. Ngày bắt đầu < ngày kết thúc
            if (startDate >= endDate)
                throw new Exception("Ngày bắt đầu phải nhỏ hơn ngày kết thúc.");

            // 3. Không cho phép ngày kết thúc trong quá khứ
            if (endDate < today)
                throw new Exception("Ngày kết thúc không được trong quá khứ.");

            // 4. Giá trị không âm
            if (dto.DiscountValue < 0 || (dto.MinOrderValue ?? 0) < 0 || (dto.MaxDiscount ?? 0) < 0)
                throw new Exception("Các giá trị số không được âm.");

            // 5. Kiểm tra nếu giảm theo phần trăm thì từ 1 đến 100
            if (dto.DiscountType == "Phần trăm" && (dto.DiscountValue < 1 || dto.DiscountValue > 100))
                throw new Exception("Giá trị phần trăm phải từ 1 đến 100.");

            // 6. Kiểm tra loại giảm giá hợp lệ
            var validTypes = new[] { "Phần trăm", "Giá trị cố định" };
            if (!validTypes.Contains(dto.DiscountType))
                throw new Exception("Kiểu giảm giá không hợp lệ.");

            // 7. Kiểm tra code trùng trong khoảng thời gian giao nhau
            var allVouchers = await _voucherRepository.GetAllAsync();

            var overlappingVoucher = allVouchers.FirstOrDefault(v =>
                v.Code == dto.Code &&
                (
                    (startDate >= v.StartDate && startDate <= v.EndDate) ||
                    (endDate >= v.StartDate && endDate <= v.EndDate) ||
                    (startDate <= v.StartDate && endDate >= v.EndDate)
                )
            );

            if (overlappingVoucher != null)
                throw new Exception("Mã voucher đã tồn tại trong khoảng thời gian trùng lặp.");

            // 8. Tạo mới - convert DateTime sang DateOnly
            var voucher = new Voucher
            {
                Code = dto.Code.Trim(),
                Description = dto.Description,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                StartDate = startDate,
                EndDate = endDate,
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

            voucher.IsDelete = true; // Đánh dấu là đã xóa
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
                Status = voucher.Status,
                IsDelete = voucher.IsDelete,
            };
        }



        public async Task<(IEnumerable<VoucherDto> data, int totalCount)> GetDeletedVouchersAsync(
    string? searchKeyword,
    string? discountType,
    int pageNumber,
    int pageSize)
        {
            var vouchers = await _voucherRepository.GetDeletedVouchersAsync(
                searchKeyword, discountType, pageNumber, pageSize);

            var totalCount = await _voucherRepository.CountDeletedVouchersAsync(
                searchKeyword, discountType);

            return (vouchers.Select(MapToDto), totalCount);
        }

        public async Task<bool> RestoreAsync(int id)
        {
            var voucher = await _voucherRepository.GetByIdAsync(id);
            if (voucher == null)
                return false;

            voucher.IsDelete = false;

            await _voucherRepository.UpdateAsync(voucher);
            await _voucherRepository.SaveChangesAsync();

            return true;
        }

    }
}
