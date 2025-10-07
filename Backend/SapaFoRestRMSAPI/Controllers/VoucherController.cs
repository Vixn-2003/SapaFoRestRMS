using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VoucherController : ControllerBase
    {
        private readonly IVoucherService _voucherService;

        public VoucherController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
     [FromQuery] string? keyword,
     [FromQuery] string? discountType,
     [FromQuery] decimal? discountValue,
     [FromQuery] DateTime? startDate,
     [FromQuery] DateTime? endDate,
     [FromQuery] decimal? minOrderValue,
     [FromQuery] decimal? maxDiscount,
     [FromQuery] string? status,
     [FromQuery] int pageNumber = 1,
     [FromQuery] int pageSize = 10)
        {
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                return BadRequest(new { error = "StartDate phải nhỏ hơn hoặc bằng EndDate." });
            }

            if ((discountValue.HasValue && discountValue < 0) ||
                (minOrderValue.HasValue && minOrderValue < 0) ||
                (maxDiscount.HasValue && maxDiscount < 0))
            {
                return BadRequest(new { error = "Các giá trị số không được phép âm." });
            }

            if (pageNumber <= 0 || pageSize <= 0)
            {
                return BadRequest(new { error = "PageNumber và PageSize phải lớn hơn 0." });
            }

            var trimmedKeyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();

            var (data, totalCount) = await _voucherService.SearchFilterPaginateAsync(
                trimmedKeyword, discountType, discountValue, startDate, endDate, minOrderValue, maxDiscount,status, pageNumber, pageSize);

            return Ok(new
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Data = data
            });
        }



        //  [GET] api/voucher/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var voucher = await _voucherService.GetByIdAsync(id);
            if (voucher == null)
                return NotFound(new { message = "Voucher not found." });

            return Ok(voucher);
        }

        //  [POST] api/voucher
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VoucherCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var created = await _voucherService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.VoucherId }, created);
            }
            catch (Exception ex)
            {
                // Trả lỗi rõ ràng cho client
                return BadRequest(new { message = ex.Message });
            }
        }

        //  [PUT] api/voucher/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] VoucherUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var updated = await _voucherService.UpdateAsync(id, dto);
                if (updated == null)
                    return NotFound(new { message = "Voucher không tồn tại." });

                return Ok(updated);
            }
            catch (Exception ex)
            {
                // 👇 Trả lỗi gọn gàng, chỉ message
                return BadRequest(new { message = ex.Message });
            }
        }


        //  [DELETE] api/voucher/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _voucherService.DeleteAsync(id);
            if (!success)
                return NotFound(new { message = "Voucher not found." });

            return Ok(new { message = "Voucher deleted successfully." });
        }

        //  Lấy danh sách voucher đã xóa 
        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeletedVouchers(
            [FromQuery] string? searchKeyword,
            [FromQuery] string? discountType,
            [FromQuery] string? status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var (data, totalCount) = await _voucherService.GetDeletedVouchersAsync(
                searchKeyword, discountType,status, pageNumber, pageSize);

            return Ok(new
            {
                TotalCount = totalCount,
                Data = data
            });
        }

        // Khôi phục voucher đã bị xóa 
        [HttpPut("restore/{id}")]
        public async Task<IActionResult> RestoreVoucher(int id)
        {
            var result = await _voucherService.RestoreAsync(id);

            if (!result)
                return NotFound(new { Message = "Voucher not found or not deleted." });

            return Ok(new { Message = "Voucher restored successfully." });
        }
    }
}
