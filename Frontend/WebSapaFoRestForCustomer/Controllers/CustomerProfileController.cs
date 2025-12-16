using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebSapaFoRestForCustomer.DTOs;
using WebSapaFoRestForCustomer.Services;

namespace WebSapaFoRestForCustomer.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerProfileController : Controller
    {
        private readonly ApiService _apiService;

        public CustomerProfileController(ApiService apiService)
        {
            _apiService = apiService;
        }

        /// <summary>
        /// Display the customer profile page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var profile = await _apiService.GetCustomerProfileAsync();
                if (profile == null)
                {
                    TempData["ErrorMessage"] = "Không thể tải thông tin khách hàng. Vui lòng thử lại sau.";
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
        /// Get current customer profile (AJAX endpoint)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var profile = await _apiService.GetCustomerProfileAsync();
                if (profile == null)
                {
                    return Json(new { success = false, message = "Không thể tải thông tin khách hàng" });
                }

                return Json(new { success = true, data = profile });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Đã xảy ra lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// Update customer profile (AJAX endpoint)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromForm] CustomerProfileUpdate request)
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
                var updatedProfile = await _apiService.UpdateCustomerProfileAsync(request);
                if (updatedProfile == null)
                {
                    return Json(new { success = false, message = "Không thể cập nhật thông tin. Vui lòng thử lại sau." });
                }

                return Json(new { success = true, message = "Cập nhật thông tin khách hàng thành công!", data = updatedProfile });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Đã xảy ra lỗi: {ex.Message}" });
            }
        }
    }
}
