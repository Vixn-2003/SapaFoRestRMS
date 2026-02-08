using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebSapaForestForStaff.DTOs.Inventory;
using WebSapaForestForStaff.Services;
using static WebSapaForestForStaff.Controllers.ManagerIngredentController;

namespace WebSapaForestForStaff.Controllers
{
    [Authorize(Policy = "Position:Inventory")]
    public class AuditInventoryController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuditInventoryController> _logger;
        private readonly IReportService _reportService;

        public AuditInventoryController(HttpClient httpClient, ILogger<AuditInventoryController> logger, IReportService reportService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _reportService = reportService;
            _httpClient.BaseAddress = new Uri("https://localhost:7096/");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/AuditInventory/GetAll");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"API call failed with status code: {response.StatusCode}");
                    TempData["ErrorMessage"] = "Không thể tải danh sách đơn kiểm kê";
                    return View("~/Views/Inventory/AuditIngredient.cshtml", new AuditInventoryPagedViewModel
                    {
                        Audits = new List<AuditInventoryDTO>(),
                        TotalItems = 0
                    });
                }

                var json = await response.Content.ReadAsStringAsync();
                var audits = JsonConvert.DeserializeObject<List<AuditInventoryDTO>>(json)
                    ?? new List<AuditInventoryDTO>();

                // ✅ SẮP XẾP THEO THỜI GIAN MỚI NHẤT (DESCENDING)
                audits = audits.OrderByDescending(a => a.CreatedAt).ToList();

                var model = new AuditInventoryPagedViewModel
                {
                    Audits = audits,
                    TotalItems = audits.Count,
                    CurrentPage = 1,
                    ItemsPerPage = 10
                };

                return View("~/Views/Inventory/AuditIngredient.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading audit inventory list");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách: " + ex.Message;
                return View("~/Views/Inventory/AuditIngredient.cshtml", new AuditInventoryPagedViewModel
                {
                    Audits = new List<AuditInventoryDTO>(),
                    TotalItems = 0
                });
            }
        }

        [HttpGet]
        [Route("AuditInventory/GetDetail/{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/AuditInventory/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy đơn kiểm kê" });
                }

                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting audit detail for ID: {id}");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        [Route("AuditInventory/ConfirmAudit/{id}")]
        public async Task<IActionResult> ConfirmAudit(string id, [FromBody] ConfirmAuditRequest request)
        {
            try
            {
                // Validate request
                if (request == null)
                {
                    _logger.LogWarning("Request body is null");
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                // Đảm bảo AuditId từ route được sử dụng
                request.AuditId = id;

                _logger.LogInformation($"Confirming audit {id} with status {request.AuditStatus} by {request.ConfirmerName}");

                var response = await _httpClient.PostAsJsonAsync(
                    $"api/AuditInventory/Confirm/{id}",
                    request
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API Error: {response.StatusCode} - {errorContent}");
                    return Json(new { success = false, message = "Không thể xác nhận đơn kiểm kê" });
                }

                return Json(new { success = true, message = "Xác nhận đơn kiểm kê thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error confirming audit for ID: {id}");
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        [Route("AuditInventory/ExportReportPdf")]
        public async Task<IActionResult> ExportReportPdf([FromBody] AuditReportRequest request)
        {
            try
            {
                _logger.LogInformation($"Exporting audit report from {request.DateFrom} to {request.DateTo}");

                var pdfBytes = await _reportService.GenerateAuditReportPdfAsync(request);

                var fileName = $"BaoCaoKiemKe_{request.DateFrom:yyyyMMdd}_{request.DateTo:yyyyMMdd}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting audit report PDF");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}