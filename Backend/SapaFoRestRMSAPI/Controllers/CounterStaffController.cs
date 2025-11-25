using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CounterStaffController : ControllerBase
    {
        public readonly ICounterStaffService _counterStaffService;

        public CounterStaffController(ICounterStaffService counterStaffService)
        {
            _counterStaffService = counterStaffService;
        }

        [HttpGet("confirmed")]
        public async Task<IActionResult> GetConfirmed(
        string? customerName = null,
        string? phone = null,
        string? timeSlot = null,
        int page = 1,
        int pageSize = 10)
        {
            var result = await _counterStaffService.GetConfirmedReservationsAsync(
                customerName, phone, timeSlot, page, pageSize);
            return Ok(result);
        }

        // GET: api/reservations/guest-seated
        [HttpGet("guest-seated")]
        public async Task<IActionResult> GetGuestSeated(
            string? customerName = null,
            string? phone = null,
            string? timeSlot = null,
            int page = 1,
            int pageSize = 10)
        {
            var result = await _counterStaffService.GetGuestSeatedReservationsAsync(
                customerName, phone, timeSlot, page, pageSize);
            return Ok(result);
        }

        // POST: api/reservations/update-status
        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateStatus(
            [FromQuery] int reservationId, [FromQuery] string newStatus)
        {
            var reservation = await _counterStaffService.
                                    UpdateReservationStatusAsync(reservationId, newStatus);

            if (reservation == null)
                return NotFound();

            return Ok();
        }
    }
}
