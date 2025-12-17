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

        // ====================== CHANGE EMAIL/PHONE (OTP) ======================
        public class SendOtpRequest
        {
            public string? Email { get; set; }
            public string? Phone { get; set; }
        }

        public class VerifyOtpRequest
        {
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? Code { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> SendEmailChangeOtp([FromBody] SendOtpRequest req)
        {
            var email = req.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { success = false, message = "Email mới không hợp lệ." });

            var ok = await _apiService.SendChangeEmailOtpAsync(email);
            return Json(new { success = ok, message = ok ? "Đã gửi OTP đến email mới." : "Không thể gửi OTP email. Vui lòng thử lại." });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmailChangeOtp([FromBody] VerifyOtpRequest req)
        {
            var email = req.Email?.Trim();
            var code = req.Code?.Trim();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
                return Json(new { success = false, message = "Thiếu email hoặc mã OTP." });

            var (success, message) = await _apiService.VerifyChangeEmailOtpAsync(email, code);
            return Json(new { success, message = success ? "Email đã được cập nhật." : (message ?? "Xác thực OTP email thất bại.") });
        }

        [HttpPost]
        public async Task<IActionResult> SendPhoneChangeOtp([FromBody] SendOtpRequest req)
        {
            var phone = req.Phone?.Trim();
            if (string.IsNullOrWhiteSpace(phone))
                return Json(new { success = false, message = "Số điện thoại mới không hợp lệ." });

            var ok = await _apiService.SendChangePhoneOtpAsync(phone);
            return Json(new { success = ok, message = ok ? "Đã gửi OTP đến số điện thoại mới." : "Không thể gửi OTP số điện thoại. Vui lòng thử lại." });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyPhoneChangeOtp([FromBody] VerifyOtpRequest req)
        {
            var phone = req.Phone?.Trim();
            var code = req.Code?.Trim();
            if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(code))
                return Json(new { success = false, message = "Thiếu số điện thoại hoặc mã OTP." });

            var (success, message) = await _apiService.VerifyChangePhoneOtpAsync(phone, code);
            return Json(new { success, message = success ? "Số điện thoại đã được cập nhật." : (message ?? "Xác thực OTP số điện thoại thất bại.") });
        }
    }
}
