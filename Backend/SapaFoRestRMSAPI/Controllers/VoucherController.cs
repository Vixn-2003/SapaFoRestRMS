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

        //  [GET] api/voucher?pageNumber=1&pageSize=10&keyword=...&discountType=...
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? keyword,
            [FromQuery] string? discountType,
            [FromQuery] decimal? discountValue,
            [FromQuery] DateOnly? startDate,
            [FromQuery] DateOnly? endDate,
            [FromQuery] decimal? minOrderValue,
            [FromQuery] decimal? maxDiscount,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var (data, totalCount) = await _voucherService.SearchFilterPaginateAsync(
                keyword, discountType, discountValue, startDate, endDate, minOrderValue, maxDiscount, pageNumber, pageSize);

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

            var created = await _voucherService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.VoucherId }, created);
        }

        //  [PUT] api/voucher/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] VoucherUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _voucherService.UpdateAsync(id, dto);
            if (updated == null)
                return NotFound(new { message = "Voucher not found." });

            return Ok(updated);
        }

        // ✅ [DELETE] api/voucher/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _voucherService.DeleteAsync(id);
            if (!success)
                return NotFound(new { message = "Voucher not found." });

            return Ok(new { message = "Voucher deleted successfully." });
        }
    }
}
