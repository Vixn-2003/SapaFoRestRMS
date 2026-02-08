using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs.CustomerManagement;
using WebSapaForestForStaff.ViewModels.CustomerManagement;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Controllers
{
    [Authorize(Roles = "Manager,Owner")]
    public class ManagerCustomerController : Controller
    {
        private readonly ICustomerManagementApiService _customerManagementApiService;
        private readonly ILogger<ManagerCustomerController> _logger;

        public ManagerCustomerController(
            ICustomerManagementApiService customerManagementApiService,
            ILogger<ManagerCustomerController> logger)
        {
            _customerManagementApiService = customerManagementApiService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CustomerFilterViewModel filter)
        {
            try
            {
                // Set default values
                if (filter.PageNumber <= 0) filter.PageNumber = 1;
                if (filter.PageSize <= 0) filter.PageSize = 20;

                // Convert ViewModel to DTO
                var filterDto = new CustomerFilterDto
                {
                    SearchKeyword = filter.Keyword,
                    IsVipOnly = filter.IsVip,
                    MinSpending = filter.MinSpending,
                    MaxSpending = filter.MaxSpending,
                    MinVisits = filter.MinVisits,
                    MaxVisits = filter.MaxVisits,
                    SortBy = filter.SortBy ?? "TotalSpending",
                    SortDirection = filter.SortDirection ?? "desc",
                    Page = filter.PageNumber,
                    PageSize = filter.PageSize
                };

                var (success, data, message) = await _customerManagementApiService.GetCustomersAsync(filterDto);

                if (!success || data == null)
                {
                    _logger.LogWarning("Failed to load customer data: {Message}", message);
                    TempData["ErrorMessage"] = message ?? "Không thể tải danh sách khách hàng.";
                    return View(new CustomerListViewModel
                    {
                        Items = new List<CustomerListItemDto>(),
                        Filters = filter,
                        Pagination = new PaginationViewModel()
                    });
                }

                // Convert response to ViewModel
                var viewModel = new CustomerListViewModel
                {
                    Items = data.Data,
                    Filters = filter,
                    Pagination = new PaginationViewModel
                    {
                        PageNumber = data.Page,
                        PageSize = data.PageSize,
                        TotalRecords = data.TotalCount,
                        TotalPages = data.TotalPages
                    }
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer list");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải danh sách khách hàng.";
                return View(new CustomerListViewModel
                {
                    Items = new List<CustomerListItemDto>(),
                    Filters = filter,
                    Pagination = new PaginationViewModel()
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var (success, data, message) = await _customerManagementApiService.GetCustomerDetailAsync(id);

                if (!success || data == null)
                {
                    TempData["ErrorMessage"] = message ?? "Không tìm thấy khách hàng.";
                    return RedirectToAction(nameof(Index));
                }

                // Convert to view model if needed, or return the data directly
                return View(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer details for ID {CustomerId}", id);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải chi tiết khách hàng.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Statistics(int customerId)
        {
            // TODO: Implement statistics endpoint if needed
            return NotFound(new { message = "Endpoint chưa được implement" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCustomerVip(int customerId, bool isVip)
        {
            try
            {
                // Get manager ID from claims
                var managerIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(managerIdClaim) || !int.TryParse(managerIdClaim, out var managerId))
                {
                    TempData["ErrorMessage"] = "Không thể xác định người quản lý.";
                    return RedirectToAction(nameof(Index));
                }

                // Get IP address
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                var dto = new CustomerVipUpdateDto
                {
                    CustomerId = customerId,
                    IsVip = isVip,
                    IsManualOverride = true // Manager can override VIP criteria
                };
                var (success, message) = await _customerManagementApiService.UpdateVipStatusAsync(dto);

                if (success)
                {
                    TempData["SuccessMessage"] = message;
                }
                else
                {
                    TempData["ErrorMessage"] = message;
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating VIP status for customer {CustomerId}", customerId);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật trạng thái VIP.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Recalculate(int customerId)
        {
            try
            {
                // Use CustomerManagement API to check VIP criteria
                var (meetsCriteria, avgAmount, reason) = await _customerManagementApiService.CheckVipCriteriaAsync(customerId);

                TempData["SuccessMessage"] = $"Đã kiểm tra tiêu chí VIP. Kết quả: {(meetsCriteria ? "Đủ điều kiện" : "Chưa đủ điều kiện")}.";
                return RedirectToAction(nameof(Details), new { id = customerId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating VIP for customer {CustomerId}", customerId);
                TempData["ErrorMessage"] = "Không thể tính lại VIP. Vui lòng thử lại.";
                return RedirectToAction(nameof(Details), new { id = customerId });
            }
        }
    }
}

