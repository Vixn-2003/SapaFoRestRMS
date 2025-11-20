using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardTableController : ControllerBase
    {
        private readonly IDashboardTableService _dashboardTableService;

        public DashboardTableController(IDashboardTableService dashboardTableService)
        {
            _dashboardTableService = dashboardTableService;
        }


        [HttpGet("List-Table")]
        public async Task<IActionResult> GetDashboardTableData([FromQuery] string? areaName,
        [FromQuery] int? floor,
        [FromQuery] string? status,
        [FromQuery] string? searchString,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
        {
            try
            {
                var data = await _dashboardTableService.GetDashboardDataAsync(areaName, floor, status, searchString, page, pageSize);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi máy chủ nội bộ: {ex.Message}" });
            }
        }

        // (1) API List (Không đổi)
        [HttpGet]
        public async Task<IActionResult> GetReservations([FromQuery] ReservationQueryParameters parameters)
        {
            var pagedResult = await _dashboardTableService.GetReservationsAsync(parameters);

            Response.Headers.Add("X-Pagination", System.Text.Json.JsonSerializer.Serialize(new
            {
                pagedResult.TotalCount,
                pagedResult.PageSize,
                pagedResult.PageNumber,
                pagedResult.HasNextPage,
                pagedResult.HasPreviousPage
            }));

            return Ok(pagedResult.Items);
        }

        // (2) API Detail (Thay đổi: Guid -> int)
        [HttpGet("{id:int}")] // Thêm ràng buộc :int
        public async Task<IActionResult> GetReservationDetail(int id)
        {
            try
            {
                var result = await _dashboardTableService.GetReservationDetailAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // (3) API Đổi trạng thái 
        [HttpPut("{id:int}/seat")] // Thêm ràng buộc :int
        public async Task<IActionResult> SeatGuest(int id)
        {
            try
            {
                await _dashboardTableService.SeatGuestAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("MenuOrder/{tableId}")]
        public async Task<IActionResult> GetMenuOrder(
     int tableId,
     [FromQuery] int? categoryId,
     [FromQuery] string? searchString)
        {
            try
            {
                var result = await _dashboardTableService.GetStaffOrderScreenAsync(tableId, categoryId, searchString);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _dashboardTableService.GetAllCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

}
