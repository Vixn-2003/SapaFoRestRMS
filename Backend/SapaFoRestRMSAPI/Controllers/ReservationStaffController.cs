using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SapaFoRestRMSAPI.Services;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationStaffController : ControllerBase
    {
        private readonly IReservationService _service;

        public ReservationStaffController(IReservationService service)
        {
            _service = service;
        }

        [HttpGet("reservations/pending-confirmed")]
        public async Task<IActionResult> GetPendingAndConfirmedReservations()
        {
            var result = await _service.GetPendingAndConfirmedReservationsAsync();
            return Ok(result);
        }

        [HttpGet("tables/by-area-all")]
        public async Task<IActionResult> GetAllTablesGroupedByArea()
        {
            var result = await _service.GetAllTablesGroupedByAreaAsync();
            return Ok(result);
        }

        [HttpGet("tables/booked")]
        public async Task<IActionResult> GetBookedTables(DateTime reservationDate, string timeSlot)
        {
            var result = await _service.GetBookedTableIdsAsync(reservationDate, timeSlot);
            return Ok(new { BookedTableIds = result });
        }

        [HttpGet("tables/suggest-by-areas")]
        public async Task<IActionResult> SuggestTablesByAreas(DateTime reservationDate, string timeSlot, int numberOfGuests, int? currentReservationId = null)
        {
            var result = await _service.SuggestTablesByAreasAsync(reservationDate, timeSlot, numberOfGuests, currentReservationId);
            return Ok(result);
        }

        [HttpPost("assign-tables")]
        public async Task<IActionResult> AssignTables([FromBody] AssignTableDto dto)
        {
            try
            {
                var result = await _service.AssignTablesAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("reset-tables/{reservationId}")]
        public async Task<IActionResult> ResetTables(int reservationId)
        {
            try
            {
                var result = await _service.ResetTablesAsync(reservationId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
