using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs;
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
                var profile = await _apiService.GetUserProfileAsync();
                if (profile == null)
                {
                    return Json(new { success = false, message = "Không thể tải thông tin người dùng" });
                }

                return Json(new { success = true, data = profile });
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
    }
}

