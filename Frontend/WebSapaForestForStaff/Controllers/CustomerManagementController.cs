using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs.CustomerManagement;
using WebSapaForestForStaff.Services.Api.Interfaces;
using WebSapaForestForStaff.ViewModels.CustomerManagement;

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
        public async Task<IActionResult> Index([FromQuery] CustomerFilterViewModel filter)
        {
            try
            {
                // Set defaults
                if (filter.PageNumber <= 0) filter.PageNumber = 1;
                if (filter.PageSize <= 0 || filter.PageSize > 100) filter.PageSize = 20;

                // Map sort options to backend format
                string sortBy = filter.SortBy;
                string sortDirection = filter.SortDirection;

                // Handle special sort options
                if (filter.SortBy == "FullNameDesc")
                {
                    sortBy = "FullName";
                    sortDirection = "desc";
                }
                else if (filter.SortBy == "FullName")
                {
                    sortBy = "FullName";
                    sortDirection = "asc";
                }
                else if (filter.SortBy == "TotalSpendingAsc")
                {
                    sortBy = "TotalSpending";
                    sortDirection = "asc";
                }
                else if (filter.SortBy == "TotalSpending")
                {
                    sortBy = "TotalSpending";
                    sortDirection = "desc";
                }
                else if (filter.SortBy == "TotalVisitsAsc")
                {
                    sortBy = "TotalVisits";
                    sortDirection = "asc";
                }
                else if (filter.SortBy == "TotalVisits")
                {
                    sortBy = "TotalVisits";
                    sortDirection = "desc";
                }
                else if (filter.SortBy == "LastVisit")
                {
                    sortBy = "LastVisit";
                    sortDirection = "desc";
                }

                // Convert ViewModel to DTO
                var filterDto = new CustomerFilterDto
                {
                    Page = filter.PageNumber,
                    PageSize = filter.PageSize,
                    SearchKeyword = filter.Keyword,
                    IsVipOnly = filter.IsVip,
                    MinSpending = filter.MinSpending,
                    MaxSpending = filter.MaxSpending,
                    MinVisits = filter.MinVisits,
                    MaxVisits = filter.MaxVisits,
                    SortBy = sortBy,
                    SortDirection = sortDirection
                };

                // Call API service
                var (success, data, message) = await _customerManagementApiService.GetCustomersAsync(filterDto);

                if (!success || data == null)
                {
                    TempData["ErrorMessage"] = message ?? "An error occurred while loading customers.";
                    // Return empty model on error
                    return View(new CustomerListViewModel
                    {
                        Filters = filter,
                        Pagination = new PaginationViewModel
                        {
                            PageNumber = filter.PageNumber,
                            PageSize = filter.PageSize,
                            TotalPages = 0,
                            TotalRecords = 0
                        }
                    });
                }

                // Build ViewModel
                var viewModel = new CustomerListViewModel
                {
                    Items = data.Data ?? new List<CustomerListItemDto>(),
                    Filters = filter,
                    Pagination = new PaginationViewModel
                    {
                        PageNumber = data.Page,
                        PageSize = data.PageSize,
                        TotalPages = data.TotalPages,
                        TotalRecords = data.TotalCount
                    }
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer list");
                TempData["ErrorMessage"] = "An error occurred while loading customers.";
                
                return View(new CustomerListViewModel
                {
                    Filters = filter,
                    Pagination = new PaginationViewModel
                    {
                        PageNumber = filter.PageNumber,
                        PageSize = filter.PageSize,
                        TotalPages = 0,
                        TotalRecords = 0
                    }
                });
            }
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

