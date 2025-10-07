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
            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new Exception("Mã voucher không được để trống.");

            var startDate = dto.StartDate.Date;  
            var endDate = dto.EndDate.Date;
            var today = DateTime.Today;

            if (startDate > endDate)
                throw new Exception("Ngày bắt đầu phải nhỏ hơn ngày kết thúc.");

            if (endDate < today)
                throw new Exception("Ngày kết thúc không được trong quá khứ.");

            if (dto.DiscountValue < 0 || (dto.MinOrderValue ?? 0) < 0 || (dto.MaxDiscount ?? 0) < 0)
                throw new Exception("Các giá trị số không được âm.");

            if (dto.DiscountType == "Phần trăm" && (dto.DiscountValue < 1 || dto.DiscountValue > 100))
                throw new Exception("Giá trị phần trăm phải từ 1 đến 100.");

            var validTypes = new[] { "Phần trăm", "Giá trị cố định" };
            if (!validTypes.Contains(dto.DiscountType))
                throw new Exception("Kiểu giảm giá không hợp lệ.");
            var allVouchers = await _voucherRepository.GetAllAsync();

            var overlappingVoucher = allVouchers.FirstOrDefault(v =>
      v.Code == dto.Code &&
      !(v.IsDelete ?? false) &&
      v.StartDate.HasValue && v.EndDate.HasValue &&
      !(
          endDate < v.StartDate.Value.Date || startDate > v.EndDate.Value.Date
      )
  );

            if (overlappingVoucher != null)
                throw new Exception("Mã voucher đã tồn tại trong khoảng thời gian trùng lặp.");


            var status = CalculateStatus(startDate, endDate);

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
                Status = status,
                IsDelete = false
            };

            await _voucherRepository.AddAsync(voucher);
            await _voucherRepository.SaveChangesAsync();

            return MapToDto(voucher);
        }

        public async Task<VoucherDto?> UpdateAsync(int id, VoucherUpdateDto dto)
        {
            var voucher = await _voucherRepository.GetByIdAsync(id);
            if (voucher == null) return null;

            var startDate = dto.StartDate.Date;
            var endDate = dto.EndDate.Date;
            var today = DateTime.Today;

            if (startDate > endDate)
                throw new Exception("Ngày bắt đầu phải nhỏ hơn ngày kết thúc.");

            if (endDate < today)
                throw new Exception("Ngày kết thúc không được trong quá khứ.");

            if (dto.DiscountValue < 0 || (dto.MinOrderValue ?? 0) < 0 || (dto.MaxDiscount ?? 0) < 0)
                throw new Exception("Các giá trị số không được âm.");

            if (dto.DiscountType == "Phần trăm" && (dto.DiscountValue < 1 || dto.DiscountValue > 100))
                throw new Exception("Giá trị phần trăm phải từ 1 đến 100.");

            var validTypes = new[] { "Phần trăm", "Giá trị cố định" };
            if (!validTypes.Contains(dto.DiscountType))
                throw new Exception("Kiểu giảm giá không hợp lệ.");

            // Kiểm tra trùng lặp thời gian với voucher khác cùng code
            var allVouchers = await _voucherRepository.GetAllAsync();

            var overlappingVoucher = allVouchers.FirstOrDefault(v =>
                v.VoucherId != id &&
                v.Code == voucher.Code &&
                !(v.IsDelete ?? false) &&
                v.StartDate.HasValue && v.EndDate.HasValue &&
                !(endDate < v.StartDate.Value.Date || startDate > v.EndDate.Value.Date)
            );

            if (overlappingVoucher != null)
                throw new Exception("Khoảng thời gian voucher bị trùng với voucher khác cùng mã.");

            // Cập nhật voucher
            voucher.Description = dto.Description;
            voucher.DiscountType = dto.DiscountType;
            voucher.DiscountValue = dto.DiscountValue;
            voucher.StartDate = startDate;
            voucher.EndDate = endDate;
            voucher.MinOrderValue = dto.MinOrderValue;
            voucher.MaxDiscount = dto.MaxDiscount;
            voucher.Status = CalculateStatus(startDate, endDate);

            await _voucherRepository.UpdateAsync(voucher);
            await _voucherRepository.SaveChangesAsync();

            return MapToDto(voucher);
        }


        public async Task<bool> DeleteAsync(int id)
        {
            var voucher = await _voucherRepository.GetByIdAsync(id);
            if (voucher == null) return false;

            voucher.IsDelete = true;
            await _voucherRepository.UpdateAsync(voucher);
            await _voucherRepository.SaveChangesAsync();

            return true;
        }

        public async Task<(IEnumerable<VoucherDto> data, int totalCount)> SearchFilterPaginateAsync(
            string? keyword,
            string? discountType,
            decimal? discountValue,
            DateTime? startDate,
            DateTime? endDate,
            decimal? minOrderValue,
            decimal? maxDiscount,
            string? status,
            int pageNumber,
            int pageSize)
        {
            var data = await _voucherRepository.GetFilteredVouchersAsync(
                keyword, discountType, discountValue, startDate, endDate, minOrderValue, maxDiscount, status, pageNumber, pageSize);

            var count = await _voucherRepository.CountFilteredVouchersAsync(
                keyword, discountType, discountValue, startDate, endDate, minOrderValue, maxDiscount, status);

            return (data.Select(MapToDto), count);
        }

        private VoucherDto MapToDto(Voucher voucher)
        {
            var hasValidDates = voucher.StartDate.HasValue && voucher.EndDate.HasValue;

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
                Status = hasValidDates
                    ? CalculateStatus(voucher.StartDate!.Value, voucher.EndDate!.Value)
                    : "Không xác định",
                IsDelete = voucher.IsDelete,
            };
        }

        public async Task<(IEnumerable<VoucherDto> data, int totalCount)> GetDeletedVouchersAsync(
            string? searchKeyword,
            string? discountType,
            string? status,
            int pageNumber,
            int pageSize)
        {
            var vouchers = await _voucherRepository.GetDeletedVouchersAsync(
                searchKeyword, discountType, status, pageNumber, pageSize);

            var totalCount = await _voucherRepository.CountDeletedVouchersAsync(
                searchKeyword, discountType, status);

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

        // Tự động chuyển trạng thái voucher theo ngày (so sánh chỉ phần Date)
        private string CalculateStatus(DateTime startDate, DateTime endDate)
        {
            var today = DateTime.Today;

            if (today < startDate.Date)
                return "Sắp ra mắt";
            else if (today >= startDate.Date && today <= endDate.Date)
                return "Đang sử dụng";
            else
                return "Hết hạn";
        }
    }
}
