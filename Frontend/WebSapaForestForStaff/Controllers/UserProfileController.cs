using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.Helpers;
using WebSapaForestForStaff.Services;

namespace WebSapaForestForStaff.Controllers
{
    [Authorize]
    public class UserProfileController : Controller
    {
        private readonly ApiService _apiService;

        public UserProfileController(ApiService apiService)
        {
            _apiService = apiService;
        }

        /// <summary>
        /// Display the user profile page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var profile = await _apiService.GetUserProfileAsync();
                if (profile == null)
                {
                    TempData["ErrorMessage"] = "Không thể tải thông tin người dùng. Vui lòng thử lại sau.";
                    return RedirectToAction("Index", "Home");
                }

                // Get positions for staff
                if (User.IsInRole("Staff"))
                {
                    var positionsClaim = User.FindFirst("Positions")?.Value;
                    if (!string.IsNullOrEmpty(positionsClaim))
                    {
                        try
                        {
                            var positions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(positionsClaim);
                            ViewBag.Positions = positions ?? new List<string>();
                        }
                        catch
                        {
                            ViewBag.Positions = new List<string>();
                        }
                    }
                    else
                    {
                        ViewBag.Positions = new List<string>();
                    }
                }

                // Set layout based on role/position
                ViewBag.Layout = ResolveLayoutForUser(User);

                return View(profile);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Đã xảy ra lỗi: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Get current user profile (AJAX endpoint)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                // Ví dụ: Lấy user ID từ extension method
                var currentUserId = this.GetCurrentUserId();
                
                var profile = await _apiService.GetUserProfileAsync();
                if (profile == null)
                {
                    return Json(new { success = false, message = "Không thể tải thông tin người dùng" });
                }

                return Json(new { success = true, data = profile, userId = currentUserId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Đã xảy ra lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Update user profile (AJAX endpoint)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromForm] UserProfileUpdateRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }

            try
            {
                var updatedProfile = await _apiService.UpdateUserProfileAsync(request);
                if (updatedProfile == null)
                {
                    return Json(new { success = false, message = "Không thể cập nhật thông tin. Vui lòng thử lại sau." });
                }

                await RefreshUserClaimsAsync(updatedProfile);

                return Json(new { success = true, message = "Cập nhật thông tin thành công!", data = updatedProfile });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Đã xảy ra lỗi: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RequestPasswordChange([FromBody] PasswordChangeRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Vui lòng nhập mật khẩu hiện tại." });
            }

            var result = await _apiService.RequestPasswordChangeAsync(request.CurrentPassword);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPasswordChange([FromBody] PasswordChangeConfirmRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = errors.FirstOrDefault() ?? "Dữ liệu không hợp lệ.", errors });
            }

            if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
            {
                return Json(new { success = false, message = "Mật khẩu xác nhận không khớp." });
            }

            var result = await _apiService.ConfirmPasswordChangeAsync(request.Code, request.NewPassword);
            return Json(new { success = result.Success, message = result.Message });
        }

        private string ResolveLayoutForUser(ClaimsPrincipal user)
        {
            // Roles take precedence
            if (user.IsInRole("Owner") || user.IsInRole("Admin"))
            {
                return "~/Views/Shared/_LayoutAdmin.cshtml";
            }

            if (user.IsInRole("Manager"))
            {
                return "~/Views/Shared/_LayoutManager.cshtml";
            }

            if (user.IsInRole("Staff"))
            {
                // Determine by PositionId/PositionIds claims
                var positionIds = new List<int>();

                var single = user.FindFirst("PositionId")?.Value;
                if (int.TryParse(single, out var pid))
                {
                    positionIds.Add(pid);
                }

                var listJson = user.FindFirst("PositionIds")?.Value;
                if (!string.IsNullOrEmpty(listJson))
                {
                    try
                    {
                        var ids = System.Text.Json.JsonSerializer.Deserialize<List<int>>(listJson);
                        if (ids != null) positionIds.AddRange(ids);
                    }
                    catch
                    {
                        // ignore parsing errors
                    }
                }

                bool hasPosition(int id) => positionIds.Any(x => x == id);

                // Position-based layouts
                // 1: Waiter/Waitress -> _waiterLayout
                if (hasPosition(1))
                {
                    return "~/Views/Shared/_counterstaffLayout.cshtml";
                }

                // 2: Cashier -> _counterstaffLayout
                if (hasPosition(2))
                {
                    return "~/Views/Shared/_counterstaffLayout.cshtml";
                }

                // 3: Kitchen -> _LayoutKitchen
                if (hasPosition(3))
                {
                    return "~/Views/Shared/_LayoutKitchen.cshtml";
                }

                // 4: Inventory -> _LayoutInventory
                if (hasPosition(4))
                {
                    return "~/Views/Shared/_LayoutInventory.cshtml";
                }

                // Staff without known position -> manager layout fallback
                return "~/Views/Shared/_LayoutManager.cshtml";
            }

            // Fallback
            return "~/Views/Shared/_LayoutAdmin.cshtml";
        }

        public class PasswordChangeRequest
        {
            [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
            public string CurrentPassword { get; set; } = null!;
        }

        public class PasswordChangeConfirmRequest
        {
            [Required(ErrorMessage = "Mã xác nhận là bắt buộc")]
            public string Code { get; set; } = null!;

            [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
            [MinLength(8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự")]
            public string NewPassword { get; set; } = null!;

            [Required(ErrorMessage = "Mật khẩu xác nhận là bắt buộc")]
            public string ConfirmPassword { get; set; } = null!;
        }

        private async Task RefreshUserClaimsAsync(User updatedUser)
        {
            var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = authResult?.Principal ?? HttpContext.User;
            if (principal?.Identity is not ClaimsIdentity identity)
            {
                return;
            }

            void ReplaceClaim(string type, string value)
            {
                var existing = identity.FindFirst(type);
                if (existing != null)
                {
                    identity.RemoveClaim(existing);
                }
                identity.AddClaim(new Claim(type, value));
            }

            ReplaceClaim(ClaimTypes.Name, updatedUser.FullName ?? string.Empty);
            ReplaceClaim(ClaimTypes.Email, updatedUser.Email ?? string.Empty);
            ReplaceClaim(ClaimTypes.MobilePhone, updatedUser.Phone ?? string.Empty);

            var authProperties = authResult?.Properties ?? new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);
        }
    }
}

