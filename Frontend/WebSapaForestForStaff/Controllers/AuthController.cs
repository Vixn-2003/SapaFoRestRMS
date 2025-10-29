using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Extensions.Logging;

namespace WebSapaForestForStaff.Controllers
{
    public class AuthController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ILogger<AuthController> logger)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:7096/")
            };
            _logger = logger;
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
                var json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/Auth/login", content);



                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonConvert.DeserializeObject<LoginResponse>(responseContent);

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, authResponse.UserId.ToString()),
                        new Claim(ClaimTypes.Name, authResponse.FullName),
                        new Claim(ClaimTypes.Email, authResponse.Email),
                        new Claim(ClaimTypes.Role, GetRoleName(authResponse.RoleId)),
                        new Claim("Token", authResponse.Token)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    _logger.LogInformation("User {Email} logged in successfully with role {Role}", authResponse.Email, authResponse.RoleId);

                    // Redirect based on role
                    var redirectUrl = authResponse.RoleId switch
                    {
                        1 => returnUrl ?? Url.Action("Index", "Admin"), // Owner -> Admin dashboard
                        2 => returnUrl ?? Url.Action("Index", "Admin"), // Admin -> Admin dashboard
                        3 => returnUrl ?? Url.Action("Index", "Users"), // Manager -> Users management
                        4 => returnUrl ?? Url.Action("Index", "Home"), // Staff -> Home dashboard
                        5 => returnUrl ?? Url.Action("Index", "Home"), // Customer -> Home dashboard
                        _ => returnUrl ?? Url.Action("Index", "Home") // Default -> Home
                    };

                    return LocalRedirect(redirectUrl);
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login");
                ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
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
            if (disposing)
            {
                _httpClient?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
