using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static BusinessAccessLayer.Services.OrderTableService;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderTableController : ControllerBase
    {
        private readonly IOrderTableService _orderTableService;
        public OrderTableController(IOrderTableService orderTableService)
        {
            _orderTableService = orderTableService;
        }

        /// <summary>
        /// Lấy danh sách bàn mà khách đã đến (lọc theo ngày & khung giờ)
        /// </summary>
        [HttpGet("tables-by-status")]
        public async Task<ActionResult<IEnumerable<Table>>> GetTablesByReservationStatus([FromQuery] string status)
        {
            if (string.IsNullOrEmpty(status))
                return BadRequest("Status parameter is required.");

            var tables = await _orderTableService.GetTablesByReservationStatusAsync(status);

            if (!tables.Any())
                return NotFound("No tables found for this status.");

            return Ok(tables);
        }


        [HttpGet("by-reservation-status")]
        public async Task<IActionResult> GetTablesByReservationStatus(
      [FromQuery] string status,
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 18)
        {
            if (string.IsNullOrWhiteSpace(status))
                return BadRequest("Status is required.");

            if (page <= 0 || pageSize <= 0)
                return BadRequest("Page and pageSize must be greater than 0.");

            var result = await _orderTableService.GetTablesByReservationStatusAsync(status, page, pageSize);

            return Ok(result);
        }

        [HttpGet("tables-by-reservation")]
        public async Task<IActionResult> GetTablesByReservation(int reservationId, string status)
        {
            var result = await _orderTableService.GetTablesByReservationIdAndStatusAsync(reservationId, status);
            if (!result.Any())
                return NotFound("No tables found for this reservation and status.");

            return Ok(result);
        }

        [HttpGet("reservation/{reservationId}")]
        public async Task<IActionResult> GetMenuForReservation(
            int reservationId,
            [FromQuery] string status, 
            [FromQuery] int? categoryId,      
            [FromQuery] string? searchString) 
        {
            try
            {
                var menu = await _orderTableService.GetMenuForReservationAsync(reservationId, status, categoryId, searchString);
                return Ok(menu);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        // GET: /api/OrderTable/floors
        // TRONG: SapaFoRestRMSAPI/Controllers/OrderTableController.cs

        // === SỬA HÀM NÀY ===
        [HttpGet("Filters/AreaNames")]
        public async Task<IActionResult> GetAreaNames()
        {
            var names = await _orderTableService.GetAreaNamesAsync();

            // Cũ (SAI): return Ok(new { data = names });
            // MỚI (ĐÚNG):
            return Ok(names);
        }

        // === VÀ SỬA HÀM NÀY ===
        [HttpGet("Filters/Floors")]
        public async Task<IActionResult> GetFloors()
        {
            var floors = await _orderTableService.GetFloorsAsync();

            // Cũ (SAI): return Ok(new { data = floors });
            // MỚI (ĐÚNG):
            return Ok(floors);
        }

        // API để tạo mã QR
        [HttpGet]
        public async Task<IActionResult> GetAllTables()
        {
            var tables = await _orderTableService.GetAllTablesAsync();
            return Ok(tables);
        }

        [HttpGet("tables")]
        public async Task<IActionResult> GetTables(
      int page = 1,
      int pageSize = 10,
      string? searchString = null,
      string? areaName = null,
      int? floor = null)
        {
            var tables = await _orderTableService.GetTablesAsync(page, pageSize, searchString, areaName, floor);
            var totalCount = await _orderTableService.GetTotalCountAsync(searchString, areaName, floor);

            return Ok(new
            {
                Data = tables,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        // API Endpoint để tạo mã QR
        // GET: /api/tables/5/qrcode
        [HttpGet("{tableId}/qrcode")]
        public async Task<IActionResult> GenerateQrForTable(int tableId)
        {
            var qrCodeBytes = await _orderTableService.GenerateQrCodeForTableAsync(tableId);

            if (qrCodeBytes == null)
            {
                return NotFound("Không tìm thấy bàn.");
            }

            // Trả về file ảnh PNG
            return File(qrCodeBytes, "image/png");
        }

        // === API MỚI: LẤY MENU CHO KHÁCH SAU KHI QUÉT QR ===
        // Endpoint này sẽ là: GET /api/OrderTable/Menu/5
        [HttpGet("MenuOrder/{tableId}")]
        public async Task<IActionResult> GetMenuForTable(int tableId,
            [FromQuery] int? categoryId,
    [FromQuery] string? searchString) 
        {
            try
            {
                // 1. Gọi service
                var menu = await _orderTableService.GetMenuForTableAsync(tableId,
                                  categoryId, searchString);
                // 2. Trả về 200 OK cùng danh sách menu
                return Ok(menu);
            }
            catch (Exception ex)
            {
                // 3. Xử lý lỗi (ví dụ: Bàn trống, "Guest Seated" chưa được set)
                // Trả về 400 Bad Request cùng thông báo lỗi
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("MenuCategories")]
        public async Task<IActionResult> GetMenuCategories()
        {
            try
            {
                var categories = await _orderTableService.GetMenuCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        // === API MỚI: NHẬN GIỎ HÀNG (ORDER) ===
        [HttpPost("SubmitOrder")]
        public async Task<IActionResult> SubmitOrder([FromBody] OrderSubmissionDto orderDto)
        {
            if (orderDto == null || orderDto.Items == null || !orderDto.Items.Any())
            {
                return BadRequest(new { message = "Giỏ hàng rỗng." });
            }

            try
            {
                // Gọi service
                var createdOrder = await _orderTableService.SubmitOrderAsync(orderDto);

                // Trả về 200 OK cùng thông tin order đã tạo
                return Ok(createdOrder);
            }
            catch (Exception ex)
            {
                // Báo lỗi 400 nếu có lỗi nghiệp vụ (ví dụ: bàn không hợp lệ)
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPost("CancelItem/{orderDetailId}")]
        public async Task<IActionResult> CancelOrderItem(int orderDetailId)
        {
            try
            {
                await _orderTableService.CancelOrderItemAsync(orderDetailId);
                return Ok(new { message = "Đã hủy món thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
