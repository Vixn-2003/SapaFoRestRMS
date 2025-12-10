using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.Customers;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/manager/customers")]
    [Authorize(Roles = "Manager,Owner")]
    public class ManagerCustomerController : ControllerBase
    {
        private readonly SapaFoRestRmsContext _context;
        private readonly ICustomerVipService _vipService;

        public ManagerCustomerController(SapaFoRestRmsContext context, ICustomerVipService vipService)
        {
            _context = context;
            _vipService = vipService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers(CancellationToken ct = default)
        {
            try
            {
                var customers = await _context.Customers
                    .AsNoTracking()
                    .Include(c => c.User)
                    .OrderBy(c => c.CustomerId)
                    .Select(c => new
                    {
                        customerId = c.CustomerId,
                        fullName = c.User != null ? c.User.FullName : null,
                        phone = c.User != null ? c.User.Phone : null,
                        loyaltyPoints = c.LoyaltyPoints ?? 0,
                        isVip = c.IsVip
                    })
                    .ToListAsync(ct);

                return Ok(customers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Không thể tải danh sách khách hàng", error = ex.Message });
            }
        }

        [HttpGet("{customerId}/statistics")]
        public async Task<IActionResult> GetCustomerStatistics(int customerId, CancellationToken ct = default)
        {
            try
            {
                var stats = await _vipService.GetStatisticsAsync(customerId, ct);
                if (stats == null)
                {
                    return NotFound(new { message = $"Không tìm thấy khách hàng với ID {customerId}" });
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Không thể lấy thống kê khách hàng", error = ex.Message });
            }
        }

        [HttpPut("{customerId}/vip")]
        public async Task<IActionResult> UpdateVipStatus(int customerId, [FromBody] UpdateVipStatusRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Dữ liệu không hợp lệ" });
            }

            try
            {
                var stats = await _vipService.UpdateVipStatusAsync(customerId, request.IsVip, ct);
                return Ok(stats);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Không thể cập nhật VIP", error = ex.Message });
            }
        }

        [HttpPost("{customerId}/recalculate")]
        public async Task<IActionResult> RecalculateVipStatus(int customerId, CancellationToken ct = default)
        {
            try
            {
                var stats = await _vipService.RefreshVipStatusAsync(customerId, ignoreManualOverride: true, ct);
                return Ok(stats);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Không thể tính lại VIP", error = ex.Message });
            }
        }
    }

    public class UpdateVipStatusRequest
    {
        public bool IsVip { get; set; }
    }
}

