using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs.Auth;
using WebSapaForestForStaff.Services;

namespace WebSapaForestForStaff.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApiService _apiService;

        public AuthController(ApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Check if already logged in
            if (HttpContext.Session.GetString("Token") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new LoginRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequest model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var response = await _apiService.LoginAsync(model);
                
                if (response != null)
                {
                    // Store user info in session
                    HttpContext.Session.SetString("UserId", response.UserId.ToString());
                    HttpContext.Session.SetString("FullName", response.FullName);
                    HttpContext.Session.SetString("Email", response.Email);
                    HttpContext.Session.SetString("RoleName", response.RoleName);
                    HttpContext.Session.SetString("RoleId", response.RoleId.ToString());

                    TempData["SuccessMessage"] = "Đăng nhập thành công!";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
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
        public IActionResult Logout()
        {
            _apiService.Logout();
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        [Route("AccessDenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
