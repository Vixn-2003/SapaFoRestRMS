using BusinessAccessLayer.DTOs.CustomerManagement;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace SapaFoRestRMSAPI.Controllers
{
    /// <summary>
    /// API Controller cho Customer Management Module
    /// UC145 - View List Customer
    /// UC146 - View Customer Detail
    /// UC147 - Update VIP Status
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Manager,Admin")] // Only Manager and Admin can access
    public class CustomerManagementController : ControllerBase
    {
        private readonly ICustomerManagementService _customerManagementService;

        public CustomerManagementController(ICustomerManagementService customerManagementService)
        {
            _customerManagementService = customerManagementService;
        }

        /// <summary>
        /// UC145 - View List Customer
        /// GET: api/customer-management
        /// </summary>
        /// <param name="searchKeyword">Search by name, phone, or email</param>
        /// <param name="isVipOnly">Filter VIP customers only</param>
        /// <param name="minSpending">Minimum total spending</param>
        /// <param name="maxSpending">Maximum total spending</param>
        /// <param name="minVisits">Minimum visit count</param>
        /// <param name="sortBy">Sort field (TotalSpending, FullName, LastVisit, TotalVisits)</param>
        /// <param name="sortDirection">Sort direction (asc/desc)</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <param name="ct">Cancellation token</param>
        [HttpGet]
        public async Task<IActionResult> GetCustomers(
            [FromQuery] string? searchKeyword,
            [FromQuery] bool? isVipOnly,
            [FromQuery] decimal? minSpending,
            [FromQuery] decimal? maxSpending,
            [FromQuery] int? minVisits,
            [FromQuery] int? maxVisits,
            [FromQuery] string sortBy = "TotalSpending",
            [FromQuery] string sortDirection = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            try
            {
                var filter = new CustomerFilterDto
                {
                    SearchKeyword = searchKeyword,
                    IsVipOnly = isVipOnly,
                    MinSpending = minSpending,
                    MaxSpending = maxSpending,
                    MinVisits = minVisits,
                    MaxVisits = maxVisits,
                    SortBy = sortBy,
                    SortDirection = sortDirection,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _customerManagementService.GetCustomersAsync(filter, ct);

                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    page = result.Page,
                    pageSize = result.PageSize,
                    totalCount = result.TotalCount,
                    totalPages = result.TotalPages
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving customers.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// UC146 - View Customer Detail
        /// GET: api/customer-management/{id}
        /// </summary>
        /// <param name="id">Customer ID</param>
        /// <param name="ct">Cancellation token</param>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomerDetail(int id, CancellationToken ct = default)
        {
            try
            {
                var customerDetail = await _customerManagementService.GetCustomerDetailAsync(id, ct);

                if (customerDetail == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Customer not found or has been removed."
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = customerDetail
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving customer details.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// UC147 - Update VIP Status
        /// PUT: api/customer-management/{id}/vip
        /// </summary>
        /// <param name="id">Customer ID</param>
        /// <param name="dto">VIP update DTO</param>
        /// <param name="ct">Cancellation token</param>
        [HttpPut("{id}/vip")]
        public async Task<IActionResult> UpdateVipStatus(
            int id, 
            [FromBody] CustomerVipUpdateDto dto, 
            CancellationToken ct = default)
        {
            try
            {
                // Validate ID match
                if (id != dto.CustomerId)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Customer ID in URL does not match the ID in request body."
                    });
                }

                // Get manager ID from claims
                var managerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(managerIdClaim, out var managerId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Manager authentication failed."
                    });
                }

                // Get IP address
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                // Update VIP status
                var (success, message) = await _customerManagementService.UpdateVipStatusAsync(
                    dto, 
                    managerId, 
                    ipAddress, 
                    ct);

                if (!success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = message
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while updating VIP status.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Check VIP Criteria for a customer
        /// GET: api/customer-management/{id}/vip-criteria
        /// </summary>
        /// <param name="id">Customer ID</param>
        /// <param name="ct">Cancellation token</param>
        [HttpGet("{id}/vip-criteria")]
        public async Task<IActionResult> CheckVipCriteria(int id, CancellationToken ct = default)
        {
            try
            {
                var (meetsCriteria, avgAmount, reason) = await _customerManagementService
                    .CheckVipCriteriaAsync(id, ct);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        customerId = id,
                        meetsCriteria = meetsCriteria,
                        averageAmountPerPerson = avgAmount,
                        reason = reason
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while checking VIP criteria.",
                    error = ex.Message
                });
            }
        }
    }
}

