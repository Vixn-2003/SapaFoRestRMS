using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services;
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
        public async Task<IActionResult> GetPendingAndConfirmedReservations(
    [FromQuery] string? status,
    [FromQuery] DateTime? date,
    [FromQuery] string? customerName,
    [FromQuery] string? phone,
    [FromQuery] string? timeSlot,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPendingAndConfirmedReservationsAsync(
                status, date, customerName, phone, timeSlot, page, pageSize);

            return Ok(result);
        }

        [HttpGet("reservations/{id}")]
        public async Task<IActionResult> GetReservationDetail(int id)
        {
            var reservation = await _service.GetReservationDetailAsync(id);
            if (reservation == null)
                return NotFound(new { message = "Không tìm thấy đặt bàn này." });

            return Ok(reservation);
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
        [HttpGet("tables/booked-with-time")]
        public async Task<IActionResult> GetBookedTablesWithTime(DateTime reservationDate, string timeSlot)
        {
            var result = await _service.GetBookedTableDetailsAsync(reservationDate, timeSlot);
            return Ok(new { BookedTables = result });
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
        [HttpPut("cancel/{id}")]
        public async Task<IActionResult> CancelReservation(int id, [FromQuery] bool refund = false)
        {
            try
            {
                var result = await _service.CancelReservationAsync(id, refund);
                return Ok(new
                {
                    Message = refund ? "Hủy đơn và hoàn cọc thành công." : "Hủy đơn không hoàn cọc thành công.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpPost("add")]
        public async Task<IActionResult> AddReservation([FromBody] ReservationCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.CreateReservationAsync(dto);

                if (result == null)
                    return Conflict(new { message = "Khách hàng đã có đơn đặt bàn trong khung giờ này." });

                return Ok(new
                {
                    message = "Tạo đơn đặt bàn thành công.",
                    data = new
                    {
                        result.ReservationId,
                        result.CustomerNameReservation,
                        result.ReservationDate,
                        result.ReservationTime,
                        result.TimeSlot,
                        result.NumberOfGuests,
                        result.Status
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPut("update/{reservationId}")]
        public async Task<IActionResult> UpdateReservation(int reservationId, [FromBody] ReservationUpdateDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Dữ liệu cập nhật không hợp lệ." });

                var result = await _service.UpdateReservationAsync(reservationId, dto);
                return Ok(new
                {
                    success = true,
                    message = "Cập nhật đơn đặt bàn thành công.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}
