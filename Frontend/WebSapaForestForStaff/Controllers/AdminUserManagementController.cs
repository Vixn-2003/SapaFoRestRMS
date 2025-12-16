using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.DTOs.UserManagement;
using WebSapaForestForStaff.Models.UserManagement;
using WebSapaForestForStaff.Services.Api.Interfaces;

namespace WebSapaForestForStaff.Controllers
{
    /// <summary>
    /// MVC Controller for Admin User Management Module
    /// Handles all user management operations for Admin role
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminUserManagementController : Controller
    {
        private readonly IUserApiService _userApiService;
        private readonly ILogger<AdminUserManagementController> _logger;

        public AdminUserManagementController(
            IUserApiService userApiService,
            ILogger<AdminUserManagementController> logger)
        {
            _userApiService = userApiService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /AdminUserManagement/Index
        /// User List Screen - displays all users with filtering and pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(UserSearchRequest? searchRequest = null)
        {
            try
            {
                searchRequest ??= new UserSearchRequest
                {
                    Page = 1,
                    PageSize = 10,
                    SortBy = "FullName",
                    SortOrder = "asc"
                };

                // Set ViewBag for form values
                ViewBag.SearchTerm = searchRequest.SearchTerm;
                ViewBag.RoleId = searchRequest.RoleId;
                ViewBag.Status = searchRequest.Status;
                ViewBag.PageSize = searchRequest.PageSize;
                ViewBag.SortBy = searchRequest.SortBy ?? "FullName";
                ViewBag.SortOrder = searchRequest.SortOrder ?? "asc";

                // Get users with pagination
                var result = await _userApiService.GetUsersWithPaginationAsync(searchRequest);
                
                if (result == null)
                {
                    // Fallback to simple list if pagination API is not available
                    var users = await _userApiService.GetUsersAsync();
                    result = new UserListResponse
                    {
                        Users = users ?? new List<User>(),
                        TotalCount = users?.Count ?? 0,
                        Page = 1,
                        PageSize = users?.Count ?? 0
                    };
                }

                // Get roles for filter dropdown
                var roles = await _userApiService.GetRolesAsync();

                var viewModel = new UserListViewModel
                {
                    UserList = result,
                    AvailableRoles = roles ?? new List<Role>(),
                    SearchRequest = searchRequest
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user list");
                TempData["ErrorMessage"] = "Lỗi khi tải danh sách người dùng";
                return View(new UserListViewModel
                {
                    UserList = new UserListResponse { Users = new List<User>() },
                    AvailableRoles = new List<Role>()
                });
            }
        }

        /// <summary>
        /// GET: /AdminUserManagement/Create
        /// Create User Screen - displays form to create new user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                var roles = await _userApiService.GetRolesAsync();
                var viewModel = new UserCreateViewModel
                {
                    AvailableRoles = roles ?? new List<Role>(),
                    Status = 0, // Default to Active
                    SendEmailNotification = true
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create user page");
                TempData["ErrorMessage"] = "Lỗi khi tải trang tạo người dùng";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /AdminUserManagement/Create
        /// Handles user creation form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                // Reload roles
                var roles = await _userApiService.GetRolesAsync();
                viewModel.AvailableRoles = roles ?? new List<Role>();
                return View(viewModel);
            }

            try
            {
                // Map ViewModel to DTO
                var createRequest = new UserCreateRequest
                {
                    FullName = viewModel.FullName,
                    Email = viewModel.Email,
                    Phone = viewModel.Phone,
                    RoleId = viewModel.RoleId,
                    Status = viewModel.Status,
                    TemporaryPassword = viewModel.TemporaryPassword,
                    SendEmailNotification = viewModel.SendEmailNotification
                };

                var success = await _userApiService.CreateUserAsync(createRequest);

                if (success)
                {
                    TempData["SuccessMessage"] = "Tạo người dùng thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Lỗi khi tạo người dùng");
                    var roles = await _userApiService.GetRolesAsync();
                    viewModel.AvailableRoles = roles ?? new List<Role>();
                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                ModelState.AddModelError("", "Lỗi kết nối. Vui lòng thử lại sau");
                var roles = await _userApiService.GetRolesAsync();
                viewModel.AvailableRoles = roles ?? new List<Role>();
                return View(viewModel);
            }
        }

        /// <summary>
        /// GET: /AdminUserManagement/Edit/{id}
        /// Edit User Screen - displays form to edit existing user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var user = await _userApiService.GetUserAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng";
                    return RedirectToAction(nameof(Index));
                }

                var roles = await _userApiService.GetRolesAsync();

                var viewModel = new UserEditViewModel
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    RoleId = user.RoleId,
                    Status = user.Status,
                    AvatarUrl = user.AvatarUrl,
                    AvailableRoles = roles ?? new List<Role>()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit user page for ID {UserId}", id);
                TempData["ErrorMessage"] = "Lỗi khi tải thông tin người dùng";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /AdminUserManagement/Edit/{id}
        /// Handles user update form submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserEditViewModel viewModel)
        {
            if (id != viewModel.UserId)
            {
                TempData["ErrorMessage"] = "Invalid user ID.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                // Reload roles
                var roles = await _userApiService.GetRolesAsync();
                viewModel.AvailableRoles = roles ?? new List<Role>();
                return View(viewModel);
            }

            try
            {
                // Map ViewModel to DTO
                var updateRequest = new UserUpdateRequest
                {
                    UserId = viewModel.UserId,
                    FullName = viewModel.FullName,
                    Email = viewModel.Email,
                    Phone = viewModel.Phone,
                    RoleId = viewModel.RoleId,
                    Status = viewModel.Status,
                    AvatarUrl = viewModel.AvatarUrl
                };

                var success = await _userApiService.UpdateUserAsync(updateRequest);

                if (success)
                {
                    TempData["SuccessMessage"] = "Cập nhật thông tin người dùng thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Lỗi khi cập nhật thông tin người dùng");
                    var roles = await _userApiService.GetRolesAsync();
                    viewModel.AvailableRoles = roles ?? new List<Role>();
                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user ID {UserId}", id);
                ModelState.AddModelError("", "Lỗi kết nối. Vui lòng thử lại sau");
                var roles = await _userApiService.GetRolesAsync();
                viewModel.AvailableRoles = roles ?? new List<Role>();
                return View(viewModel);
            }
        }

        /// <summary>
        /// GET: /AdminUserManagement/Details/{id}
        /// User Detail Screen - displays detailed information about a user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userDetails = await _userApiService.GetUserDetailsAsync(id);
                if (userDetails == null)
                {
                    // Fallback to simple user if details API is not available
                    var user = await _userApiService.GetUserAsync(id);
                    if (user == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy người dùng";
                        return RedirectToAction(nameof(Index));
                    }
                    
                    // Create a basic UserDetailsResponse from User
                    userDetails = new UserDetailsResponse
                    {
                        UserId = user.UserId,
                        FullName = user.FullName,
                        Email = user.Email,
                        Phone = user.Phone,
                        RoleId = user.RoleId,
                        RoleName = user.RoleName ?? "Unknown",
                        Status = user.Status,
                        AvatarUrl = user.AvatarUrl,
                        CreatedAt = user.CreatedAt
                    };
                }

                var viewModel = new UserDetailViewModel
                {
                    UserDetails = userDetails
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for ID {UserId}", id);
                TempData["ErrorMessage"] = "Lỗi khi tải thông tin người dùng";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /AdminUserManagement/Deactivate
        /// Deactivates a user (called via AJAX from modal)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate([FromBody] DeactivateUserRequest request)
        {
            try
            {
                if (request == null || request.UserId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid request data." });
                }

                // Change status to 1 (Inactive) - 0 = Active, 1 = Inactive
                var success = await _userApiService.ChangeUserStatusAsync(request.UserId, 1);

                if (success)
                {
                    return Ok(new { success = true, message = "User deactivated successfully." });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Failed to deactivate user." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", request?.UserId);
                return StatusCode(500, new { success = false, message = "An error occurred while deactivating user." });
            }
        }

        /// <summary>
        /// POST: /AdminUserManagement/Delete/{id}
        /// Deletes a user (soft delete)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _userApiService.GetUserAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                var success = await _userApiService.DeleteUserAsync(id);

                if (success)
                {
                    return Json(new { success = true, message = $"User '{user.FullName}' deleted successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to delete user." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return Json(new { success = false, message = "An error occurred while deleting user." });
            }
        }
    }

    /// <summary>
    /// DTO for deactivate user request
    /// </summary>
    public class DeactivateUserRequest
    {
        public int UserId { get; set; }
        public string? Reason { get; set; }
    }
}

