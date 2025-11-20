using BusinessAccessLayer.DTOs.ReservationDepositDto;
using BusinessAccessLayer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationDepositController : ControllerBase
    {
        private readonly ReservationDepositService _depositService;

        public ReservationDepositController(ReservationDepositService depositService)
        {
            _depositService = depositService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddDeposit([FromForm] ReservationDepositDto dto)
        {
            var result = await _depositService.AddDepositAsync(dto);
            return Ok(result);
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateDeposit(int id, [FromForm] ReservationDepositDto dto)
        {
            var result = await _depositService.UpdateDepositAsync(id, dto);
            return Ok(result);
        }
        [HttpGet("by-reservation/{reservationId}")]
        public async Task<IActionResult> GetByReservationId(int reservationId)
        {
            var result = await _depositService.GetDepositsByReservationIdAsync(reservationId);
            return Ok(result);
        }

        [HttpDelete("{depositId}")]
        public async Task<IActionResult> DeleteDeposit(int depositId)
        {
            await _depositService.DeleteDepositAsync(depositId);
            return Ok(new { message = "Xóa giao dịch cọc thành công." });
        }

    }
}
