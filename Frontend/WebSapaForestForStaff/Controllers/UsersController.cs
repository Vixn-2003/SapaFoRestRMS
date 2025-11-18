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
                
                // Set ViewBag for form values
                ViewBag.SearchTerm = searchRequest.SearchTerm;
                ViewBag.RoleId = searchRequest.RoleId;
                ViewBag.Status = searchRequest.Status;
                ViewBag.PageSize = searchRequest.PageSize;
                ViewBag.SortBy = searchRequest.SortBy ?? "FullName";
                ViewBag.SortOrder = searchRequest.SortOrder ?? "asc";
                
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
        public async Task<IActionResult> CreateStaff()
        {
            try
            {
                // Load positions for the dropdown
                var positions = await LoadPositionsAsync();
                ViewBag.Positions = positions ?? new List<Position>();

                // Set default hire date to today
                var model = new CreateStaffRequest
                {
                    HireDate = DateOnly.FromDateTime(DateTime.Now)
                };

                return View(model);
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi khi tải danh sách vị trí";
                ViewBag.Positions = new List<Position>();
                return View(new CreateStaffRequest { HireDate = DateOnly.FromDateTime(DateTime.Now) });
            }
        }

        private async Task<List<Position>?> LoadPositionsAsync()
        {
            return await _apiService.GetPositionsAsync();
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Owner")]
        public async Task<IActionResult> CreateUnified()
        {
            var roles = await _apiService.GetRolesAsync();
            ViewBag.Roles = roles ?? new List<Role>();
            ViewBag.Positions = await LoadPositionsAsync() ?? new List<Position>();
            var model = new UnifiedCreateUserRequest
            {
                HireDate = DateOnly.FromDateTime(DateTime.Now)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,Owner")]
        public async Task<IActionResult> CreateUnified(UnifiedCreateUserRequest model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _apiService.GetRolesAsync() ?? new List<Role>();
                ViewBag.Positions = await LoadPositionsAsync() ?? new List<Position>();
                return View(model);
            }

            try
            {
                bool ok = false;
                if (model.RoleId == 3) // Manager
                {
                    var req = new CreateManagerRequest { FullName = model.FullName, Email = model.Email, Phone = model.Phone };
                    ok = await _apiService.CreateManagerAsync(req);
                }
                else if (model.RoleId == 4) // Staff
                {
                    var req = new CreateStaffRequest
                    {
                        FullName = model.FullName,
                        Email = model.Email,
                        Phone = model.Phone,
                        HireDate = model.HireDate ?? DateOnly.FromDateTime(DateTime.Now),
                        SalaryBase = model.SalaryBase ?? 0,
                        PositionIds = model.PositionIds ?? new List<int>()
                    };
                    ok = await _apiService.CreateStaffAsync(req);
                }
                else
                {
                    var req = new UserCreateRequest { FullName = model.FullName, Email = model.Email, Phone = model.Phone, RoleId = model.RoleId, Status = 0 };
                    ok = await _apiService.CreateUserAsync(req);
                }

                if (ok)
                {
                    TempData["SuccessMessage"] = "Tạo người dùng thành công!";
                    return RedirectToAction("Index");
                }

                TempData["ErrorMessage"] = "Không thể tạo người dùng.";
                ViewBag.Roles = await _apiService.GetRolesAsync() ?? new List<Role>();
                ViewBag.Positions = await LoadPositionsAsync() ?? new List<Position>();
                return View(model);
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi kết nối. Vui lòng thử lại sau";
                ViewBag.Roles = await _apiService.GetRolesAsync() ?? new List<Role>();
                ViewBag.Positions = await LoadPositionsAsync() ?? new List<Position>();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager,Admin,Owner")]
        public async Task<IActionResult> CreateStaff(CreateStaffRequest model)
        {
            if (!ModelState.IsValid)
            {
                // Reload positions for the view
                var positions = await LoadPositionsAsync();
                ViewBag.Positions = positions ?? new List<Position>();
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
                    var positions = await LoadPositionsAsync();
                    ViewBag.Positions = positions ?? new List<Position>();
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi kết nối. Vui lòng thử lại sau");
                var positions = await LoadPositionsAsync();
                ViewBag.Positions = positions ?? new List<Position>();
                return View(model);
            }
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
        public async Task<IActionResult> Edit(int id, UserUpdateRequest model)
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
                    return RedirectToActionPreservingFilters();
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
            //try
            //{
            //    var user = await _apiService.GetUserAsync(id);
            //    if (user == null)
            //    {
            //        TempData["ErrorMessage"] = "Không tìm thấy người dùng";
            //        return RedirectToAction("Index");
            //    }

            //    var model = new PasswordResetRequest
            //    {
            //        UserId = user.UserId,
            //        FullName = user.FullName,
            //        Email = user.Email
            //    };

            //    return View(model);
            //}
            //catch
            //{
            //    TempData["ErrorMessage"] = "Lỗi khi tải thông tin người dùng";
            return RedirectToAction("Index");
            //}
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
                    return RedirectToActionPreservingFilters();
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
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _apiService.GetUserAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng";
                    return RedirectToAction("Index");
                }

                var success = await _apiService.DeleteUserAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Xóa người dùng \"{user.FullName}\" thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa người dùng này";
                }
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa người dùng";
            }

            return RedirectToActionPreservingFilters();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,Owner")]
        public async Task<IActionResult> ChangeStatus(int id, int status)
        {
            try
            {
                var user = await _apiService.GetUserAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng";
                    return RedirectToAction("Index");
                }

                var success = await _apiService.ChangeUserStatusAsync(id, status);
                if (success)
                {
                    var statusText = status == 0 ? "kích hoạt" : "vô hiệu hóa";
                    TempData["SuccessMessage"] = $"Đã {statusText} người dùng \"{user.FullName}\" thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể thay đổi trạng thái người dùng này";
                }
            }
            catch
            {
                TempData["ErrorMessage"] = "Lỗi khi thay đổi trạng thái người dùng";
            }

            return RedirectToActionPreservingFilters();
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

            return RedirectToActionPreservingFilters();
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

            return RedirectToActionPreservingFilters();
        }

        /// <summary>
        /// Redirect to Index action while preserving current search filters from Request
        /// </summary>
        private IActionResult RedirectToActionPreservingFilters()
        {
            var searchTerm = Request.Query["searchTerm"].ToString();
            var roleIdStr = Request.Query["roleId"].ToString();
            var statusStr = Request.Query["status"].ToString();
            var pageStr = Request.Query["page"].ToString();
            var pageSizeStr = Request.Query["pageSize"].ToString();
            
            int? roleId = null;
            if (!string.IsNullOrEmpty(roleIdStr) && int.TryParse(roleIdStr, out int roleIdVal))
                roleId = roleIdVal;
            
            int? status = null;
            if (!string.IsNullOrEmpty(statusStr) && int.TryParse(statusStr, out int statusVal))
                status = statusVal;
            
            int page = 1;
            if (!string.IsNullOrEmpty(pageStr) && int.TryParse(pageStr, out int pageVal))
                page = pageVal;
            
            int pageSize = 10;
            if (!string.IsNullOrEmpty(pageSizeStr) && int.TryParse(pageSizeStr, out int pageSizeVal))
                pageSize = pageSizeVal;

            var searchRequest = new UserSearchRequest
            {
                SearchTerm = searchTerm,
                RoleId = roleId,
                Status = status,
                Page = page,
                PageSize = pageSize,
                SortBy = Request.Query["sortBy"].ToString() ?? "FullName",
                SortOrder = Request.Query["sortOrder"].ToString() ?? "asc"
            };

            return RedirectToAction("Index", searchRequest);
        }
    }
}

