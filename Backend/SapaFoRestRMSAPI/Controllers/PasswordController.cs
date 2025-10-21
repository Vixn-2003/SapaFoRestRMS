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
            await _passwordService.RequestChangeAsync(dto, ct);
            return Ok();
        }

        [HttpPost("change/confirm")]
        [Authorize]
        public async Task<IActionResult> Change([FromBody] VerifyChangePasswordDto dto, CancellationToken ct)
        {
            await _passwordService.ChangeAsync(dto, ct);
            return Ok();
        }
    }
}


