using Microsoft.AspNetCore.Mvc;
using WebSapaFoRestForCustomer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Extensions.Logging;

namespace WebSapaFoRestForCustomer.Controllers
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

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            // If already authenticated via cookie, redirect to home
            if (User?.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new OtpRequestDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestOtp(OtpRequestDto model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View("Login", model);
            }

            try
            {
                var json = JsonConvert.SerializeObject(model.Phone);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/Customer/send-otp-login", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Mã OTP đã được gửi đến số điện thoại của bạn.";
                    TempData["Phone"] = model.Phone;
                    return View("VerifyOtp", new VerifyOtpDto { Phone = model.Phone });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, "Không thể gửi mã OTP. Vui lòng thử lại.");
                return View("Login", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during OTP request");
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra. Vui lòng thử lại.");
                return View("Login", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpDto model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var verifyDto = new
                {
                    Phone = model.Phone,
                    Code = model.Code
                };

                var json = JsonConvert.SerializeObject(verifyDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/Customer/verify-otp-login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonConvert.DeserializeObject<LoginResponse>(responseContent);

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, authResponse.UserId.ToString()),
                        new Claim(ClaimTypes.Name, authResponse.FullName),
                        new Claim(ClaimTypes.Email, authResponse.Email),
                        new Claim(ClaimTypes.Role, "Customer"),
                        new Claim("Token", authResponse.Token)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24) // Longer session for customers
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    _logger.LogInformation("Customer {Phone} logged in successfully", model.Phone);

                    return LocalRedirect(returnUrl ?? Url.Action("Index", "Home"));
                }

                ModelState.AddModelError(string.Empty, "Mã OTP không đúng hoặc đã hết hạn. Vui lòng thử lại.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during OTP verification");
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra. Vui lòng thử lại.");
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

        [HttpGet]
        public IActionResult ResendOtp(string phone)
        {
            if (string.IsNullOrEmpty(phone))
            {
                return RedirectToAction("Login");
            }

            return View("VerifyOtp", new VerifyOtpDto { Phone = phone });
        }
    }

    public class LoginResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Token { get; set; } = null!;
        public int RoleId { get; set; }
    }

    public class VerifyOtpDto
    {
        public string Phone { get; set; } = null!;
        public string Code { get; set; } = null!;
    }
}
