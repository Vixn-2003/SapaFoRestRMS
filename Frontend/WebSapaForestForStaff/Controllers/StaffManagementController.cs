using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs.Staff;
using WebSapaForestForStaff.Models.StaffViewModels;
using WebSapaForestForStaff.Services.Api.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebSapaForestForStaff.Controllers
{
    /// <summary>
    /// MVC Controller for Staff Management Module
    /// UC55 - View List Staff
    /// UC56 - Update Staff
    /// UC57 - Deactivate / Delete Staff
    /// Create Staff
    /// </summary>
    [Authorize(Policy = "Manager")] // Only Manager and above can access
    public class StaffManagementController : Controller
    {
        private readonly IStaffManagementApiService _staffManagementApiService;
        private readonly ILogger<StaffManagementController> _logger;

        public StaffManagementController(
            IStaffManagementApiService staffManagementApiService,
            ILogger<StaffManagementController> logger)
        {
            _staffManagementApiService = staffManagementApiService;
            _logger = logger;
        }

        /// <summary>
        /// UC55 - View List Staff
        /// GET: /StaffManagement/Index
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View();
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
                // Get positions for dropdown
                var (success, positions, message) = await _staffManagementApiService.GetActivePositionsAsync();

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
                TempData["ErrorMessage"] = "An error occurred while loading the page.";
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
            try
            {
                if (!ModelState.IsValid)
                {
                    // Reload positions
                    var (_, positions, _) = await _staffManagementApiService.GetActivePositionsAsync();
                    viewModel.AvailablePositions = positions ?? new List<PositionDto>();
                    return View(viewModel);
                }

                // Map ViewModel to DTO
                var dto = new StaffCreateDto
                {
                    FullName = viewModel.FullName,
                    Email = viewModel.Email,
                    Phone = viewModel.Phone,
                    BaseSalary = viewModel.BaseSalary,
                    HireDate = viewModel.HireDate,
                    DepartmentId = viewModel.DepartmentId,
                    PositionIds = viewModel.SelectedPositionIds,
                    RoleId = viewModel.RoleId,
                    Password = viewModel.Password,
                    AvatarUrl = viewModel.AvatarUrl
                };

                var (success, staffId, message) = await _staffManagementApiService.CreateStaffAsync(dto);

                if (!success)
                {
                    TempData["ErrorMessage"] = message;
                    // Reload positions
                    var (_, positions, _) = await _staffManagementApiService.GetActivePositionsAsync();
                    viewModel.AvailablePositions = positions ?? new List<PositionDto>();
                    return View(viewModel);
                }

                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating staff");
                TempData["ErrorMessage"] = "An error occurred while creating staff.";
                return RedirectToAction(nameof(Index));
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
                var (success, staffDetail, message) = await _staffManagementApiService.GetStaffDetailAsync(id);

                if (!success || staffDetail == null)
                {
                    TempData["ErrorMessage"] = message ?? "Staff not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Get positions for dropdown
                var (_, positions, _) = await _staffManagementApiService.GetActivePositionsAsync();

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
                    SelectedPositionIds = staffDetail.Positions.Select(p => p.PositionId).ToList(),
                    AvailablePositions = positions ?? new List<PositionDto>(),
                    AvatarUrl = staffDetail.AvatarUrl
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit staff page for ID {StaffId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading staff details.";
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
            try
            {
                if (id != viewModel.StaffId)
                {
                    TempData["ErrorMessage"] = "Invalid staff ID.";
                    return RedirectToAction(nameof(Index));
                }

                if (!ModelState.IsValid)
                {
                    // Reload positions
                    var (_, positions, _) = await _staffManagementApiService.GetActivePositionsAsync();
                    viewModel.AvailablePositions = positions ?? new List<PositionDto>();
                    return View(viewModel);
                }

                // Map ViewModel to DTO
                var dto = new StaffUpdateDto
                {
                    StaffId = viewModel.StaffId,
                    FullName = viewModel.FullName,
                    Phone = viewModel.Phone,
                    BaseSalary = viewModel.BaseSalary,
                    Status = viewModel.Status,
                    PositionIds = viewModel.SelectedPositionIds,
                    AvatarUrl = viewModel.AvatarUrl
                };

                var (success, message) = await _staffManagementApiService.UpdateStaffAsync(id, dto);

                if (!success)
                {
                    TempData["ErrorMessage"] = message;
                    // Reload positions
                    var (_, positions, _) = await _staffManagementApiService.GetActivePositionsAsync();
                    viewModel.AvailablePositions = positions ?? new List<PositionDto>();
                    return View(viewModel);
                }

                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating staff ID {StaffId}", id);
                TempData["ErrorMessage"] = "An error occurred while updating staff.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// UC57 - Deactivate Staff
        /// POST: /StaffManagement/Deactivate
        /// Called via AJAX from JavaScript
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Deactivate([FromBody] StaffDeactivateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid request data." });
                }

                var (success, message) = await _staffManagementApiService.DeactivateStaffAsync(dto.StaffId, dto);

                return Ok(new { success = success, message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating staff {StaffId}", dto.StaffId);
                return StatusCode(500, new { success = false, message = "An error occurred while deactivating staff." });
            }
        }

        /// <summary>
        /// API endpoint for loading staff list via AJAX
        /// POST: /StaffManagement/LoadStaffList
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> LoadStaffList([FromBody] StaffFilterDto filter)
        {
            try
            {
                var (success, data, message) = await _staffManagementApiService.GetStaffListAsync(filter);

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
                _logger.LogError(ex, "Error loading staff list");
                return StatusCode(500, new { success = false, message = "An error occurred while loading staff list." });
            }
        }

        /// <summary>
        /// Get staff detail via AJAX
        /// GET: /StaffManagement/GetStaffDetail/{id}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetStaffDetail(int id)
        {
            try
            {
                var (success, data, message) = await _staffManagementApiService.GetStaffDetailAsync(id);

                if (!success || data == null)
                {
                    return BadRequest(new { success = false, message = message });
                }

                return Ok(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff detail for ID {StaffId}", id);
                return StatusCode(500, new { success = false, message = "An error occurred while getting staff details." });
            }
        }
    }
}

