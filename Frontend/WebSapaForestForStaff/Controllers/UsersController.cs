using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.DTOs.UserManagement;
using WebSapaForestForStaff.Services;

namespace WebSapaForestForStaff.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ApiService _apiService;

        public UsersController(ApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Owner")]
        public async Task<IActionResult> Index(UserSearchRequest? searchRequest = null)
        {
            try
            {
                searchRequest ??= new UserSearchRequest();
                
                var result = await _apiService.GetUsersWithPaginationAsync(searchRequest);
                if (result == null)
                {
                    // Fallback to simple list if pagination API is not available
                    var users = await _apiService.GetUsersAsync();
                    var fallbackResult = new UserListResponse
                    {
                        Users = users ?? new List<User>(),
                        TotalCount = users?.Count ?? 0,
                        Page = 1,
                        PageSize = users?.Count ?? 0
                    };
                    return View(fallbackResult);
                }
                
                return View(result);
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi khi tải danh sách người dùng";
                return View(new UserListResponse { Users = new List<User>() });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Owner")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userDetails = await _apiService.GetUserDetailsAsync(id);
                if (userDetails == null)
                {
                    // Fallback to simple user if details API is not available
                    var user = await _apiService.GetUserAsync(id);
                    if (user == null)
                    {
                        return NotFound();
                    }
                    return View(user);
                }
                return View(userDetails);
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi khi tải thông tin người dùng";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult CreateManager()
        {
            return View(new CreateManagerRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> CreateManager(CreateManagerRequest model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var success = await _apiService.CreateManagerAsync(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Tạo tài khoản quản lý thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Lỗi khi tạo tài khoản quản lý");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi kết nối. Vui lòng thử lại sau");
                return View(model);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Manager,Admin,Owner")]
        public IActionResult CreateStaff()
        {
            return View(new CreateStaffRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager,Admin,Owner")]
        public async Task<IActionResult> CreateStaff(CreateStaffRequest model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var success = await _apiService.CreateStaffAsync(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Tạo tài khoản nhân viên thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Lỗi khi tạo tài khoản nhân viên");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi kết nối. Vui lòng thử lại sau");
                return View(model);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Owner")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var user = await _apiService.GetUserAsync(id);
                if (user == null)
                {
                    return NotFound();
                }
                return View(user);
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi khi tải thông tin người dùng";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,Owner")]
        public async Task<IActionResult> Edit(User model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var success = await _apiService.UpdateUserAsync(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Cập nhật thông tin người dùng thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Lỗi khi cập nhật thông tin người dùng");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi kết nối. Vui lòng thử lại sau");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,Owner")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _apiService.DeleteUserAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "Xóa người dùng thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Lỗi khi xóa người dùng";
                }
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi kết nối. Vui lòng thử lại sau";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,Owner")]
        public async Task<IActionResult> ChangeStatus(int id, int status)
        {
            try
            {
                var success = await _apiService.ChangeUserStatusAsync(id, status);
                if (success)
                {
                    TempData["SuccessMessage"] = "Thay đổi trạng thái người dùng thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Lỗi khi thay đổi trạng thái người dùng";
                }
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi kết nối. Vui lòng thử lại sau";
            }

            return RedirectToAction("Index");
        }

        // Enhanced User Management Actions
        [HttpGet]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var roles = await _apiService.GetRolesAsync();
                ViewBag.Roles = roles ?? new List<Role>();
                return View(new UserCreateRequest());
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi khi tải danh sách vai trò";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> Create(UserCreateRequest model)
        {
            if (!ModelState.IsValid)
            {
                try
                {
                    var roles = await _apiService.GetRolesAsync();
                    ViewBag.Roles = roles ?? new List<Role>();
                }
                catch { }
                return View(model);
            }

            try
            {
                var success = await _apiService.CreateUserAsync(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Tạo người dùng thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Lỗi khi tạo người dùng");
                    try
                    {
                        var roles = await _apiService.GetRolesAsync();
                        ViewBag.Roles = roles ?? new List<Role>();
                    }
                    catch { }
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi kết nối. Vui lòng thử lại sau");
                try
                {
                    var roles = await _apiService.GetRolesAsync();
                    ViewBag.Roles = roles ?? new List<Role>();
                }
                catch { }
                return View(model);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Owner")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var user = await _apiService.GetUserAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                var roles = await _apiService.GetRolesAsync();
                ViewBag.Roles = roles ?? new List<Role>();

                var updateRequest = new UserUpdateRequest
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone,
                    RoleId = user.RoleId,
                    Status = user.Status
                };

                return View(updateRequest);
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi khi tải thông tin người dùng";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,Owner")]
        public async Task<IActionResult> Edit(UserUpdateRequest model)
        {
            if (!ModelState.IsValid)
            {
                try
                {
                    var roles = await _apiService.GetRolesAsync();
                    ViewBag.Roles = roles ?? new List<Role>();
                }
                catch { }
                return View(model);
            }

            try
            {
                var success = await _apiService.UpdateUserAsync(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Cập nhật thông tin người dùng thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Lỗi khi cập nhật thông tin người dùng");
                    try
                    {
                        var roles = await _apiService.GetRolesAsync();
                        ViewBag.Roles = roles ?? new List<Role>();
                    }
                    catch { }
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi kết nối. Vui lòng thử lại sau");
                try
                {
                    var roles = await _apiService.GetRolesAsync();
                    ViewBag.Roles = roles ?? new List<Role>();
                }
                catch { }
                return View(model);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            try
            {
                var user = await _apiService.GetUserAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                var request = new PasswordResetRequest
                {
                    UserId = id,
                    SendEmailNotification = true
                };

                return View(request);
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi khi tải thông tin người dùng";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> ResetPassword(PasswordResetRequest model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var success = await _apiService.ResetUserPasswordAsync(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Lỗi khi đặt lại mật khẩu");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi kết nối. Vui lòng thử lại sau");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> BulkDelete(int[] userIds)
        {
            if (userIds == null || userIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một người dùng để xóa";
                return RedirectToAction("Index");
            }

            try
            {
                int successCount = 0;
                foreach (var userId in userIds)
                {
                    var success = await _apiService.DeleteUserAsync(userId);
                    if (success) successCount++;
                }

                if (successCount == userIds.Length)
                {
                    TempData["SuccessMessage"] = $"Xóa thành công {successCount} người dùng!";
                }
                else if (successCount > 0)
                {
                    TempData["WarningMessage"] = $"Xóa thành công {successCount}/{userIds.Length} người dùng!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa người dùng nào!";
                }
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa người dùng";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> BulkChangeStatus(int[] userIds, int status)
        {
            if (userIds == null || userIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một người dùng để thay đổi trạng thái";
                return RedirectToAction("Index");
            }

            try
            {
                int successCount = 0;
                foreach (var userId in userIds)
                {
                    var success = await _apiService.ChangeUserStatusAsync(userId, status);
                    if (success) successCount++;
                }

                if (successCount == userIds.Length)
                {
                    TempData["SuccessMessage"] = $"Thay đổi trạng thái thành công cho {successCount} người dùng!";
                }
                else if (successCount > 0)
                {
                    TempData["WarningMessage"] = $"Thay đổi trạng thái thành công cho {successCount}/{userIds.Length} người dùng!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể thay đổi trạng thái người dùng nào!";
                }
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi khi thay đổi trạng thái người dùng";
            }

            return RedirectToAction("Index");
        }
    }
}
