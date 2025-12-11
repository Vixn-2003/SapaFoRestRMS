using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs.CustomerManagement;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Controllers
{
    /// <summary>
    /// MVC Controller cho Customer Management Module
    /// UC145 - View List Customer
    /// UC146 - View Customer Detail
    /// UC147 - Update VIP Status
    /// </summary>
    [Authorize(Policy = "Manager")] // Only Manager and above can access
    public class CustomerManagementController : Controller
    {
        private readonly ICustomerManagementApiService _customerManagementApiService;
        private readonly ILogger<CustomerManagementController> _logger;

        public CustomerManagementController(
            ICustomerManagementApiService customerManagementApiService,
            ILogger<CustomerManagementController> logger)
        {
            _customerManagementApiService = customerManagementApiService;
            _logger = logger;
        }

        /// <summary>
        /// UC145 - View List Customer
        /// GET: /CustomerManagement/Index
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// UC146 - View Customer Detail
        /// GET: /CustomerManagement/Detail/{id}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                var (success, data, message) = await _customerManagementApiService.GetCustomerDetailAsync(id);

                if (!success || data == null)
                {
                    TempData["ErrorMessage"] = message ?? "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer detail for ID {CustomerId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading customer details.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// UC147 - Update VIP Status
        /// POST: /CustomerManagement/UpdateVipStatus
        /// Called via AJAX from JavaScript
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateVipStatus([FromBody] CustomerVipUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid request data." });
                }

                var (success, message) = await _customerManagementApiService.UpdateVipStatusAsync(dto);

                return Ok(new { success = success, message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating VIP status for customer {CustomerId}", dto.CustomerId);
                return StatusCode(500, new { success = false, message = "An error occurred while updating VIP status." });
            }
        }

        /// <summary>
        /// Check VIP Criteria for a customer
        /// GET: /CustomerManagement/CheckVipCriteria/{id}
        /// Called via AJAX from JavaScript
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckVipCriteria(int id)
        {
            try
            {
                var (success, data, message) = await _customerManagementApiService.CheckVipCriteriaAsync(id);

                if (!success)
                {
                    return BadRequest(new { success = false, message = message });
                }

                return Ok(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking VIP criteria for customer {CustomerId}", id);
                return StatusCode(500, new { success = false, message = "An error occurred while checking VIP criteria." });
            }
        }

        /// <summary>
        /// API endpoint for loading customer list via AJAX
        /// POST: /CustomerManagement/LoadCustomers
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> LoadCustomers([FromBody] CustomerFilterDto filter)
        {
            try
            {
                var (success, data, message) = await _customerManagementApiService.GetCustomersAsync(filter);

                if (!success || data == null)
                {
                    return BadRequest(new { success = false, message = message });
                }

                return Ok(new
                {
                    success = true,
                    data = data.Data,
                    page = data.Page,
                    pageSize = data.PageSize,
                    totalCount = data.TotalCount,
                    totalPages = data.TotalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customers");
                return StatusCode(500, new { success = false, message = "An error occurred while loading customers." });
            }
        }
    }
}

