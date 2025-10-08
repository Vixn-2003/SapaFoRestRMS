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

        public AuthController(IAuthService authService, IUserManagementService userManagementService, IExternalAuthService externalAuthService)
        {
            _authService = authService;
            _userManagementService = userManagementService;
            _externalAuthService = externalAuthService;
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
    }
}


