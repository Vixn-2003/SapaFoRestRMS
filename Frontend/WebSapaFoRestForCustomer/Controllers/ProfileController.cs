using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebSapaFoRestForCustomer.Services;

namespace WebSapaFoRestForCustomer.Controllers
{
    [Authorize(Roles = "Customer")]
    public class ProfileController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(ApiService apiService, ILogger<ProfileController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var profile = await _apiService.GetCustomerProfileAsync();
                return View(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer profile");
                TempData["ErrorMessage"] = "Không thể tải thông tin cá nhân. Vui lòng thử lại.";
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(CustomerProfileUpdate model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            try
            {
                var success = await _apiService.UpdateCustomerProfileAsync(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể cập nhật thông tin. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer profile");
                TempData["ErrorMessage"] = "Có lỗi xảy ra. Vui lòng thử lại.";
            }

            return RedirectToAction("Index");
        }
    }
}
