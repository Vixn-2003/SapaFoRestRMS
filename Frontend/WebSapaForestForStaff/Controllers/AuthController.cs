using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs.Auth;
using WebSapaForestForStaff.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace WebSapaForestForStaff.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApiService _apiService;

        public AuthController(ApiService apiService)
        {
            _apiService = apiService;
        }
        public string ReturnUrl { get; set; }

        [HttpGet]
        public IActionResult Login()
        {
            // If already authenticated via cookie, redirect by role
            if (User?.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
                if (User.IsInRole("Manager"))
                {
                    return RedirectToAction("Index", "Users");
                }
                if (User.IsInRole("Staff"))
                {
                    return RedirectToAction("Index", "Home");
                }
                return RedirectToAction("Index", "Home");
            }

            // If token exists in session (fallback), go home
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
            Console.WriteLine(model.email);

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

                    // Build claims identity for cookie auth
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, response.UserId.ToString()),
                        new Claim(ClaimTypes.Name, response.FullName ?? response.Email),
                        new Claim(ClaimTypes.Email, response.Email),
                        new Claim(ClaimTypes.Role, string.IsNullOrWhiteSpace(response.RoleName) ? "Staff" : response.RoleName)
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
                    {
                        IsPersistent = true,
                        AllowRefresh = true
                    });

                    TempData["SuccessMessage"] = "Đăng nhập thành công!";

                    // Redirect based on role name
                    var role = (response.RoleName ?? string.Empty).Trim();
                    if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    if (string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("Index", "Users");
                    }
                    if (string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("Index", "Home");
                    }

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
        public async Task<IActionResult> Logout()
        {
            _apiService.Logout();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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
