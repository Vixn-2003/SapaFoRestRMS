using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.Auth;
using BusinessAccessLayer.DTOs.UserManagement;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserManagementService _userManagementService;
        private readonly IExternalAuthService _externalAuthService;
        private readonly IPhoneAuthService _phoneAuthService;

        public AuthController(IAuthService authService, IUserManagementService userManagementService, IExternalAuthService externalAuthService,
          IPhoneAuthService phoneAuthService)
        {
            _authService = authService;
            _userManagementService = userManagementService;
            _externalAuthService = externalAuthService;
            _phoneAuthService = phoneAuthService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while logging in" });
            }
        }

        public class RefreshTokenRequest { public string RefreshToken { get; set; } = string.Empty; }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token is required" });
            }
            try
            {
                var resp = await _authService.RefreshTokenAsync(req.RefreshToken);
                return Ok(resp);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while refreshing token" });
            }
        }

        [HttpPost("google-login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> GoogleLogin([FromBody] GoogleLoginRequest request, CancellationToken ct)
        {
            var result = await _externalAuthService.GoogleLoginAsync(request, ct);
            return Ok(result);
        }

        [HttpPost("admin/create-manager")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> CreateManager([FromBody] CreateManagerRequest request, CancellationToken ct)
        {
            var adminUserId = int.Parse(User.FindFirst("userId")!.Value);
            var (userId, tempPassword) = await _userManagementService.CreateManagerAsync(request, adminUserId, ct);
            return Ok(new { userId, tempPassword });
        }

        [HttpPost("manager/create-staff")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult<object>> CreateStaff([FromBody] CreateStaffRequest request, CancellationToken ct)
        {
            var managerUserId = int.Parse(User.FindFirst("userId")!.Value);
            var (userId, staffId, tempPassword) = await _userManagementService.CreateStaffAsync(request, managerUserId, ct);
            return Ok(new { userId, staffId, tempPassword });
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // For JWT-based auth, logout is handled client-side by discarding the token.
            // This endpoint exists to standardize the flow and can be extended to support revocation.
            return Ok(new { message = "Logged out" });
        }

        public class RequestOtpDto { public string Phone { get; set; } = string.Empty; }
        public class VerifyOtpDto { public string Phone { get; set; } = string.Empty; public string Code { get; set; } = string.Empty; }

        [HttpPost("request-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestOtp([FromBody] RequestOtpDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Phone)) return BadRequest(new { message = "Phone is required" });
            try
            {
                await _phoneAuthService.RequestOtpAsync(dto.Phone, ct);
                return Ok(new { message = "OTP sent" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> VerifyOtp([FromBody] VerifyOtpDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Phone) || string.IsNullOrWhiteSpace(dto.Code))
            {
                return BadRequest(new { message = "Phone and code are required" });
            }
            try
            {
                var response = await _phoneAuthService.VerifyOtpAsync(dto.Phone, dto.Code, ct);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}


