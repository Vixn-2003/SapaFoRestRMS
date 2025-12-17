using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs.Staff;
using WebSapaForestForStaff.Models.StaffViewModels;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Controllers
{
    /// <summary>
    /// MVC Controller for Staff Management Module
    /// Responsibilities:
    /// - Render views only (Index, Create, Edit)
    /// - Handle form submissions (Create, Edit)
    /// - Provide AJAX endpoints that delegate to API service
    /// </summary>
    [Authorize(Policy = "Manager")]
    public class StaffManagementController : Controller
    {
        private readonly IStaffManagementApiService _staffApiService;
        private readonly ILogger<StaffManagementController> _logger;

        public StaffManagementController(
            IStaffManagementApiService staffApiService,
            ILogger<StaffManagementController> logger)
        {
            _staffApiService = staffApiService;
            _logger = logger;
        }

        /// <summary>
        /// UC55 - View List Staff (renders view only, data loaded via AJAX)
        /// GET: /StaffManagement/Index
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            // Pure view rendering - no data loading here
            // JavaScript will call GetStaffList AJAX endpoint
            return View();
        }

        /// <summary>
        /// AJAX endpoint: Get paginated staff list with filters
        /// POST: /StaffManagement/GetStaffList
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetStaffList([FromBody] StaffFilterDto filter)
        {
            try
            {
                // Validate and normalize filter
                filter ??= new StaffFilterDto();
                filter.Page = filter.Page > 0 ? filter.Page : 1;
                filter.PageSize = filter.PageSize > 0 ? filter.PageSize : 20;
                filter.SortBy = string.IsNullOrWhiteSpace(filter.SortBy) ? "HireDate" : filter.SortBy;
                filter.SortDirection = string.IsNullOrWhiteSpace(filter.SortDirection) ? "desc" : filter.SortDirection;

                var (success, data, message) = await _staffApiService.GetStaffListAsync(filter);

                if (!success || data == null)
                {
                    return Ok(new
                    {
                        success = false,
                        message = message ?? "Không thể tải danh sách nhân viên",
                        data = Array.Empty<object>(),
                        page = filter.Page,
                        pageSize = filter.PageSize,
                        totalCount = 0,
                        totalPages = 0
                    });
                }

                // Return normalized response
                return Ok(new
                {
                    success = true,
                    message = (string?)null,
                    data = data.Data,
                    page = data.Page,
                    pageSize = data.PageSize,
                    totalCount = data.TotalCount,
                    totalPages = data.TotalPages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetStaffList endpoint");
                return Ok(new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi tải danh sách nhân viên",
                    data = Array.Empty<object>(),
                    page = 1,
                    pageSize = 20,
                    totalCount = 0,
                    totalPages = 0
                });
            }
        }

        /// <summary>
        /// Create Staff - GET
        /// GET: /StaffManagement/Create
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                var (success, positions, message) = await _staffApiService.GetActivePositionsAsync();

                var viewModel = new StaffCreateViewModel
                {
                    AvailablePositions = positions ?? new List<PositionDto>(),
                    HireDate = DateOnly.FromDateTime(DateTime.Now)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create staff page");
                TempData["ErrorMessage"] = "Lỗi khi tải trang tạo nhân viên";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Create Staff - POST
        /// POST: /StaffManagement/Create
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StaffCreateViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                var (_, positions, _) = await _staffApiService.GetActivePositionsAsync();
                viewModel.AvailablePositions = positions ?? new List<PositionDto>();
                return View(viewModel);
            }

            try
            {
                // Always set RoleId to Staff (4) for new staff
                // BaseSalary is calculated from selected positions on client side
                var dto = new StaffCreateDto
                {
                    FullName = viewModel.FullName,
                    Email = viewModel.Email,
                    Phone = viewModel.Phone,
                    BaseSalary = viewModel.BaseSalary, // From selected position
                    HireDate = viewModel.HireDate,
                    DepartmentId = viewModel.DepartmentId > 0 ? viewModel.DepartmentId : 1, // Default to 1
                    PositionId = viewModel.PositionId, // Single position only
                    RoleId = 4, // Always Staff
                    Password = viewModel.Password,
                    AvatarUrl = viewModel.AvatarUrl
                };

                var (success, staffId, message) = await _staffApiService.CreateStaffAsync(dto);

                if (!success)
                {
                    ModelState.AddModelError("", message ?? "Lỗi khi tạo nhân viên");
                    var (_, positions, _) = await _staffApiService.GetActivePositionsAsync();
                    viewModel.AvailablePositions = positions ?? new List<PositionDto>();
                    return View(viewModel);
                }

                TempData["SuccessMessage"] = message ?? "Tạo nhân viên thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating staff");
                ModelState.AddModelError("", "Đã xảy ra lỗi khi tạo nhân viên");
                var (_, positions, _) = await _staffApiService.GetActivePositionsAsync();
                viewModel.AvailablePositions = positions ?? new List<PositionDto>();
                return View(viewModel);
            }
        }

        /// <summary>
        /// UC56 - Edit Staff - GET
        /// GET: /StaffManagement/Edit/{id}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var (success, staffDetail, message) = await _staffApiService.GetStaffDetailAsync(id);

                if (!success || staffDetail == null)
                {
                    TempData["ErrorMessage"] = message ?? "Không tìm thấy nhân viên";
                    return RedirectToAction(nameof(Index));
                }

                var (_, positions, _) = await _staffApiService.GetActivePositionsAsync();

                var viewModel = new StaffEditViewModel
                {
                    StaffId = staffDetail.StaffId,
                    FullName = staffDetail.FullName,
                    Email = staffDetail.Email,
                    Phone = staffDetail.Phone,
                    BaseSalary = staffDetail.BaseSalary,
                    Status = staffDetail.Status,
                    HireDate = staffDetail.HireDate,
                    DepartmentName = staffDetail.DepartmentName,
                    PositionId = staffDetail.Positions.FirstOrDefault()?.PositionId ?? 0, // Single position only
                    AvailablePositions = positions ?? new List<PositionDto>(),
                    AvatarUrl = staffDetail.AvatarUrl
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit page for staff ID {StaffId}", id);
                TempData["ErrorMessage"] = "Lỗi khi tải thông tin nhân viên";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// UC56 - Edit Staff - POST
        /// POST: /StaffManagement/Edit/{id}
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StaffEditViewModel viewModel)
        {
            if (id != viewModel.StaffId)
            {
                TempData["ErrorMessage"] = "ID không hợp lệ";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                var (_, positions, _) = await _staffApiService.GetActivePositionsAsync();
                viewModel.AvailablePositions = positions ?? new List<PositionDto>();
                return View(viewModel);
            }

            try
            {
                var dto = new StaffUpdateDto
                {
                    StaffId = viewModel.StaffId,
                    FullName = viewModel.FullName,
                    Phone = viewModel.Phone,
                    BaseSalary = viewModel.BaseSalary,
                    Status = viewModel.Status,
                    PositionId = viewModel.PositionId, // Single position only
                    AvatarUrl = viewModel.AvatarUrl
                };

                var (success, message) = await _staffApiService.UpdateStaffAsync(id, dto);

                if (!success)
                {
                    ModelState.AddModelError("", message ?? "Lỗi khi cập nhật nhân viên");
                    var (_, positions, _) = await _staffApiService.GetActivePositionsAsync();
                    viewModel.AvailablePositions = positions ?? new List<PositionDto>();
                    return View(viewModel);
                }

                TempData["SuccessMessage"] = message ?? "Cập nhật nhân viên thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating staff ID {StaffId}", id);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật nhân viên");
                var (_, positions, _) = await _staffApiService.GetActivePositionsAsync();
                viewModel.AvailablePositions = positions ?? new List<PositionDto>();
                return View(viewModel);
            }
        }

        /// <summary>
        /// UC57 - Deactivate Staff (AJAX endpoint)
        /// POST: /StaffManagement/Deactivate
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate([FromBody] StaffDeactivateDto dto)
        {
            try
            {
                if (dto == null || dto.StaffId <= 0)
                {
                    return Ok(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                var (success, message) = await _staffApiService.DeactivateStaffAsync(dto.StaffId, dto);

                return Ok(new { success, message = message ?? (success ? "Ngừng hoạt động thành công" : "Lỗi khi ngừng hoạt động") });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating staff {StaffId}", dto?.StaffId);
                return Ok(new { success = false, message = "Đã xảy ra lỗi khi ngừng hoạt động nhân viên" });
            }
        }
    }
}
