using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.Auth;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordController : ControllerBase
    {
        private readonly IPasswordService _passwordService;

        public PasswordController(IPasswordService passwordService)
        {
            _passwordService = passwordService;
        }

        [HttpPost("reset/request")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestReset([FromBody] RequestPasswordResetDto dto, CancellationToken ct)
        {
            await _passwordService.RequestResetAsync(dto, ct);
            return Ok();
        }

        [HttpPost("reset/verify")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyReset([FromBody] VerifyPasswordResetDto dto, CancellationToken ct)
        {
            var newPassword = await _passwordService.VerifyResetAsync(dto, ct);
            return Ok(new { newPassword });
        }

        [HttpPost("change/request")]
        [Authorize]
        public async Task<IActionResult> RequestChange([FromBody] RequestChangePasswordDto dto, CancellationToken ct)
        {
            var userIdClaim = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            dto.UserId = userId;

            try
            {
                await _passwordService.RequestChangeAsync(dto, ct);
                return Ok(new { message = "Verification code sent" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("change/confirm")]
        [Authorize]
        public async Task<IActionResult> Change([FromBody] VerifyChangePasswordDto dto, CancellationToken ct)
        {
            var userIdClaim = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            dto.UserId = userId;

            try
            {
                await _passwordService.ChangeAsync(dto, ct);
                return Ok(new { message = "Password changed" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}


