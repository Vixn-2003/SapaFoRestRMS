using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using WebSapaForestForStaff.Services;

namespace WebSapaForestForStaff.Controllers
{
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;
        private readonly ApiService _apiService;

        public AuthController(ILogger<AuthController> logger, ApiService apiService)
        {
            _logger = logger;
            _apiService = apiService;
        }
        public string ReturnUrl { get; set; }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            // If already authenticated via cookie, redirect by role
            if (User?.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Owner"))
                {
                    return RedirectToAction("Index", "Admin"); // Owner has admin privileges
                }
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
                if (User.IsInRole("Customer"))
                {
                    return RedirectToAction("Index", "Home"); // Customer redirected to home
                }
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequest model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                _logger.LogInformation(" Login attempt: Email = {Email}, Password(raw) = {Password}",
                    model.Email, model.Password);
                var authResponse = await _apiService.LoginAsync(model);
                if (authResponse != null)
                {
                    // Tạo claims để xác thực cookie
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, authResponse.UserId.ToString()),
                new Claim(ClaimTypes.Name, authResponse.FullName ?? ""),
                new Claim(ClaimTypes.Email, authResponse.Email ?? ""),
                new Claim(ClaimTypes.Role, GetRoleName(authResponse.RoleId)),
                new Claim("Token", authResponse.Token ?? "")
            };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // Token is already saved to Session inside ApiService.LoginAsync; keep claim copy above

                    _logger.LogInformation(" User {Email} logged in successfully with role {RoleId}",
                        authResponse.Email, authResponse.RoleId);

                    // 🔁 Redirect theo Role
                    var redirectUrl = authResponse.RoleId switch
                    {
                        1 => returnUrl ?? Url.Action("Index", "Admin"),
                        2 => returnUrl ?? Url.Action("Index", "Admin"),
                        3 => returnUrl ?? Url.Action("Index", "Users"),
                        4 => returnUrl ?? Url.Action("Index", "Home"),
                        5 => returnUrl ?? Url.Action("Index", "Home"),
                        _ => returnUrl ?? Url.Action("Index", "Home")
                    };

                    return LocalRedirect(redirectUrl);
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }


        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _apiService.Logout();
            return RedirectToAction("Login");
        }

        private string GetRoleName(int roleId)
        {
            return roleId switch
            {
                1 => "Owner",
                2 => "Admin",
                3 => "Manager", 
                4 => "Staff",
                5 => "Customer",
                _ => "Staff"
            };
        }

        [HttpGet]
        [Route("AccessDenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
