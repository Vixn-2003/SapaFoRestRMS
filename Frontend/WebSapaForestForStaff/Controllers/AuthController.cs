using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs.Auth;
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
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // If already authenticated via cookie, redirect by role
            if (User?.Identity?.IsAuthenticated == true)
            {
                var sessionToken = HttpContext.Session.GetString("Token") ?? User.FindFirst("Token")?.Value;
                var refreshToken = HttpContext.Session.GetString("RefreshToken");

                if (string.IsNullOrEmpty(sessionToken))
                {
                    await ForceSignOutAsync();
                    return RedirectToAction("Login");
                }

                if (IsTokenExpired(sessionToken))
                {
                    if (string.IsNullOrEmpty(refreshToken) || !await _apiService.TryRefreshTokenAsync())
                    {
                        await ForceSignOutAsync();
                        return RedirectToAction("Login");
                    }

                    var refreshedToken = HttpContext.Session.GetString("Token");
                    if (!string.IsNullOrEmpty(refreshedToken))
                    {
                        await UpdateTokenClaimAsync(refreshedToken);
                    }
                }

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
                    return RedirectToAction("Index", "HomeManager");
                }
                if (User.IsInRole("Staff"))
                {
                    // Check Staff positions by PositionId
                    // 1: Waiter/Waitress      -> DashboardTable
                    // 2: Cashier              -> DashboardTable
                    // 3: Kitchen Staff        -> KischenDisplay
                    // 4: Inventory Staff      -> DashboardInventory
                    var positionIdClaim = User.FindFirst("PositionId")?.Value;
                    if (int.TryParse(positionIdClaim, out var primaryPositionId))
                    {
                        switch (primaryPositionId)
                        {
                            case 1: // Waiter/Waitress
                            case 2: // Cashier
                                return RedirectToAction("Index", "DashboardTable");
                            case 3: // Kitchen Staff
                                return RedirectToAction("Index", "KitchenDisplay");
                            case 4: // Inventory Staff
                                return RedirectToAction("Index", "DashboardInventory");
                        }
                    }

                    // Fallback: try list of PositionIds if available
                    var positionIdsJson = User.FindFirst("PositionIds")?.Value;
                    if (!string.IsNullOrEmpty(positionIdsJson))
                    {
                        try
                        {
                            var positionIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(positionIdsJson) ?? new();
                            if (positionIds.Contains(1) || positionIds.Contains(2))
                            {
                                return RedirectToAction("Index", "DashboardTable");
                            }
                            if (positionIds.Contains(3))
                            {
                                return RedirectToAction("Index", "KitchenDisplay");
                            }
                            if (positionIds.Contains(4))
                            {
                                return RedirectToAction("Index", "DashboardInventory");
                            }
                        }
                        catch
                        {
                            // ignore and fall through
                        }
                    }

                    // Default page for Staff if no matching position
                    return RedirectToAction("Index", "TableManage");
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
                        new Claim(ClaimTypes.MobilePhone, authResponse.Phone ?? ""),
                        new Claim("Token", authResponse.Token ?? "")
                    };

                    // Lưu position ids & names vào claims nếu có
                    if (authResponse.PositionIds != null && authResponse.PositionIds.Any())
                    {
                        // Primary PositionId (dùng cho phân quyền nhanh)
                        claims.Add(new Claim("PositionId", authResponse.PositionIds.First().ToString()));

                        // Lưu list PositionIds dạng JSON
                        var positionIdsJson = System.Text.Json.JsonSerializer.Serialize(authResponse.PositionIds);
                        claims.Add(new Claim("PositionIds", positionIdsJson));
                    }

                    // (Tuỳ chọn) vẫn lưu Positions (tên) để hiển thị ở màn profile, nếu backend trả về
                    if (authResponse.Positions != null && authResponse.Positions.Any())
                    {
                        var positionsJson = System.Text.Json.JsonSerializer.Serialize(authResponse.Positions);
                        claims.Add(new Claim("Positions", positionsJson));
                    }

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

                    // 🔁 Redirect theo Role và Position
                    string redirectUrl;
                    
                    // Check Staff positions (RoleId = 4) theo PositionId
                    if (authResponse.RoleId == 4 && authResponse.PositionIds != null && authResponse.PositionIds.Any())
                    {
                        var positionIds = authResponse.PositionIds;

                        // Id = 1,2 (Waiter/Waitress, Cashier) -> DashboardTable
                        if (positionIds.Contains(1) || positionIds.Contains(2))
                        {
                            redirectUrl = returnUrl ?? Url.Action("Index", "DashboardTable");
                        }
                        // Id = 3 (Kitchen Staff) -> KischenDisplay
                        else if (positionIds.Contains(3))
                        {
                            redirectUrl = returnUrl ?? Url.Action("Index", "KitchenDisplay");
                        }
                        // Id = 4 (Inventory Staff) -> DashboardInventory
                        else if (positionIds.Contains(4))
                        {
                            redirectUrl = returnUrl ?? Url.Action("Index", "DashboardInventory");
                        }
                        else
                        {
                            // Staff nhưng không match position cụ thể -> về trang TableManage mặc định
                            redirectUrl = returnUrl ?? Url.Action("Index", "TableManage");
                        }
                    }
                    else
                    {
                        // Các Role khác (Owner/Admin/Manager/Customer)
                        redirectUrl = authResponse.RoleId switch
                        {
                            1 => returnUrl ?? Url.Action("Index", "Admin"),
                            2 => returnUrl ?? Url.Action("Index", "Admin"),
                            3 => returnUrl ?? Url.Action("Index", "HomeManager"),
                            4 => returnUrl ?? Url.Action("Index", "TableManage"),
                            5 => returnUrl ?? Url.Action("Index", "Home"),
                            _ => returnUrl ?? Url.Action("Index", "Home")
                        };
                    }

                    return LocalRedirect(redirectUrl);
                }

                ModelState.AddModelError("Password", "Email hoặc mật khẩu không đúng");
                return View(model);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Login failed: {Message}", ex.Message);
                // Hiển thị error dưới thanh password
                ModelState.AddModelError("Password", ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login");
                ModelState.AddModelError("Password", "Đã xảy ra lỗi. Vui lòng thử lại.");
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

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new DTOs.Auth.ForgotPasswordRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(DTOs.Auth.ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            try
            {
                var success = await _apiService.ForgotPasswordAsync(request.Email);
                if (success)
                {
                    TempData["SuccessMessage"] = "Mã xác nhận đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư.";
                    return RedirectToAction("ResetPassword", new { email = request.Email });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi. Vui lòng thử lại.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ForgotPassword");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi. Vui lòng thử lại.");
            }

            return View(request);
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            var model = new DTOs.Auth.ResetPasswordRequest();
            if (!string.IsNullOrEmpty(email))
            {
                model.Email = email;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(DTOs.Auth.ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            try
            {
                var success = await _apiService.ResetPasswordAsync(request);
                if (success)
                {
                    TempData["SuccessMessage"] = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập với mật khẩu mới.";
                    return RedirectToAction("Login");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi. Vui lòng thử lại.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ResetPassword");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi. Vui lòng thử lại.");
            }

            return View(request);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private static bool IsTokenExpired(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                {
                    return true;
                }

                var jwt = handler.ReadJwtToken(token);
                var expiration = jwt.ValidTo;
                return expiration <= DateTime.UtcNow.AddMinutes(-1);
            }
            catch
            {
                return true;
            }
        }

        private async Task UpdateTokenClaimAsync(string token)
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = authenticateResult?.Principal ?? HttpContext.User;
            if (principal?.Identity is not ClaimsIdentity identity)
            {
                return;
            }

            var existingTokenClaim = identity.FindFirst("Token");
            if (existingTokenClaim != null)
            {
                identity.RemoveClaim(existingTokenClaim);
            }
            identity.AddClaim(new Claim("Token", token));

            var authProperties = authenticateResult?.Properties ?? new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);
        }

        private async Task ForceSignOutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _apiService.Logout();
        }
    }
}
