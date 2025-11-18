using System;
using System.ComponentModel.DataAnnotations;
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

                // Set layout based on role
                if (User.IsInRole("Manager"))
                {
                    ViewBag.Layout = "~/Views/Shared/_LayoutManager.cshtml";
                }
                else if (User.IsInRole("Staff"))
                {
                    // Staff with Cashier position uses different layout
                    var positionsClaim = User.FindFirst("Positions")?.Value;
                    var isCashier = false;
                    if (!string.IsNullOrEmpty(positionsClaim))
                    {
                        try
                        {
                            var positions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(positionsClaim);
                            isCashier = positions?.Any(p => string.Equals(p, "Cashier", StringComparison.OrdinalIgnoreCase)) ?? false;
                        }
                        catch { }
                    }
                    
                    // Cashier uses payment layout, other staff use manager layout
                    ViewBag.Layout = isCashier ? "~/Views/Shared/_Layout.cshtml" : "~/Views/Shared/_LayoutManager.cshtml";
                }
                else
                {
                    ViewBag.Layout = "~/Views/Shared/_LayoutAdmin.cshtml";
                }

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
        public async Task<IActionResult> UpdateProfile([FromBody] UserProfileUpdateRequest request)
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
    }
}

