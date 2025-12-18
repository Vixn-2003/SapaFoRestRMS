using BusinessAccessLayer.DTOs.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using WebSapaForestForStaff.DTOs.Inventory;
using WebSapaForestForStaff.Services;

namespace WebSapaForestForStaff.Controllers
{
    [Authorize(Policy = "Position:Inventory")]
    public class ExportInventoryController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ExportReportService _reportService;
        public ExportInventoryController(HttpClient httpClient, ExportReportService reportService)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:7096/")
            };
            _reportService = reportService;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new ExportManagementViewModel();

            try
            {
                var response = await _httpClient.GetAsync("api/ExportIngredient");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var exports = JsonSerializer.Deserialize<List<StockTransactionInventoryDTO>>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    viewModel.ExportData = exports ?? new List<StockTransactionInventoryDTO>();
                }
                else
                {
                    viewModel.ErrorMessage = $"Không có dữ liệu nào";
                }
            }
            catch (Exception ex)
            {
                viewModel.ErrorMessage = $"Không có dữ liệu nào";
            }

            return View("~/Views/Menu/ExportManagement.cshtml", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ExportReport([FromBody] ExportReportRequest request)
        {
            try
            {
                // Lấy thông tin người dùng từ Claims
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nhân viên kho";

                // Lấy dữ liệu từ API
                var response = await _httpClient.GetAsync("api/ExportIngredient");
                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest("Không thể lấy dữ liệu xuất kho");
                }

                var content = await response.Content.ReadAsStringAsync();
                var allExports = JsonSerializer.Deserialize<List<StockTransactionInventoryDTO>>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new List<StockTransactionInventoryDTO>();

                // Lọc theo khoảng thời gian
                DateTime fromDate, toDate;
                switch (request.Period)
                {
                    case "today":
                        fromDate = DateTime.Today;
                        toDate = DateTime.Today.AddDays(1).AddSeconds(-1);
                        break;
                    case "3days":
                        fromDate = DateTime.Today.AddDays(-3);
                        toDate = DateTime.Today.AddDays(1).AddSeconds(-1);
                        break;
                    case "7days":
                        fromDate = DateTime.Today.AddDays(-7);
                        toDate = DateTime.Today.AddDays(1).AddSeconds(-1);
                        break;
                    case "30days":
                        fromDate = DateTime.Today.AddDays(-30);
                        toDate = DateTime.Today.AddDays(1).AddSeconds(-1);
                        break;
                    default:
                        return BadRequest("Khoảng thời gian không hợp lệ");
                }

                var filteredExports = allExports.Where(x =>
                    x.TransactionDate.HasValue &&
                    x.TransactionDate.Value >= fromDate &&
                    x.TransactionDate.Value <= toDate
                ).ToList();

                // Tính toán thống kê
                var reportData = new ExportReportDTO
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    CreatedBy = userName,
                    TotalTransactions = filteredExports.Count,
                    Transactions = filteredExports
                };

                // Top nguyên liệu
                reportData.TopIngredients = filteredExports
                    .GroupBy(x => new { x.IngredientId, x.IngredientName, x.UnitName })
                    .Select(g => new TopIngredientDTO
                    {
                        IngredientName = g.Key.IngredientName,
                        TotalQuantity = g.Sum(x => x.Quantity),
                        UnitName = g.Key.UnitName
                    })
                    .OrderByDescending(x => x.TotalQuantity)
                    .Take(10)
                    .ToList();

                // Xuất theo kho
                reportData.WarehouseStats = filteredExports
                    .GroupBy(x => new { x.WarehouseId, x.WarehouseName })
                    .Select(g => new WarehouseStatDTO
                    {
                        WarehouseName = g.Key.WarehouseName,
                        TransactionCount = g.Count()
                    })
                    .OrderByDescending(x => x.TransactionCount)
                    .ToList();

                // Tính xuất bất thường và so sánh (chỉ cho báo cáo hôm nay hoặc 7 ngày)
                if (request.Period == "today" || request.Period == "7days")
                {
                    var today = DateTime.Today;
                    var sevenDaysAgo = today.AddDays(-7);

                    var todayExports = allExports.Where(x =>
                        x.TransactionDate.HasValue &&
                        x.TransactionDate.Value.Date == today
                    ).ToList();

                    var week7Exports = allExports.Where(x =>
                        x.TransactionDate.HasValue &&
                        x.TransactionDate.Value >= sevenDaysAgo
                    ).ToList();

                    // Tính trung bình 7 ngày
                    var avgByIngredient = week7Exports
                        .GroupBy(x => new { x.IngredientId, x.IngredientName, x.UnitName })
                        .Select(g => new
                        {
                            g.Key.IngredientId,
                            g.Key.IngredientName,
                            g.Key.UnitName,
                            AvgQuantity = g.Sum(x => x.Quantity) / 7m
                        })
                        .ToDictionary(x => x.IngredientId);

                    // Xuất bất thường
                    reportData.AbnormalExports = todayExports
                        .GroupBy(x => new { x.IngredientId, x.IngredientName, x.UnitName })
                        .Select(g => new
                        {
                            g.Key.IngredientId,
                            g.Key.IngredientName,
                            g.Key.UnitName,
                            TodayQuantity = g.Sum(x => x.Quantity)
                        })
                        .Where(x => avgByIngredient.ContainsKey(x.IngredientId))
                        .Select(x =>
                        {
                            var avg = avgByIngredient[x.IngredientId].AvgQuantity;
                            var percent = avg > 0 ? ((x.TodayQuantity / avg - 1) * 100) : 0;
                            return new AbnormalExportDTO
                            {
                                IngredientName = x.IngredientName,
                                TodayQuantity = x.TodayQuantity,
                                AvgQuantity = avg,
                                PercentChange = percent,
                                UnitName = x.UnitName
                            };
                        })
                        .Where(x => x.PercentChange > 50)
                        .OrderByDescending(x => x.PercentChange)
                        .ToList();

                    // So sánh tiêu hao
                    reportData.Comparisons = todayExports
                        .GroupBy(x => new { x.IngredientId, x.IngredientName, x.UnitName })
                        .Select(g => new
                        {
                            g.Key.IngredientId,
                            g.Key.IngredientName,
                            g.Key.UnitName,
                            TodayQuantity = g.Sum(x => x.Quantity)
                        })
                        .Select(x =>
                        {
                            var avg = avgByIngredient.ContainsKey(x.IngredientId)
                                ? avgByIngredient[x.IngredientId].AvgQuantity
                                : 0;
                            var percent = avg > 0 ? ((x.TodayQuantity / avg - 1) * 100) : 0;
                            return new ComparisonDTO
                            {
                                IngredientName = x.IngredientName,
                                TodayQuantity = x.TodayQuantity,
                                AvgQuantity = avg,
                                PercentChange = percent,
                                UnitName = x.UnitName
                            };
                        })
                        .OrderByDescending(x => x.TodayQuantity)
                        .ToList();
                }

                // Tạo file PDF
                var pdfBytes = _reportService.GenerateExportReport(reportData);

                var fileName = $"BaoCaoXuatKho_{fromDate:ddMMyyyy}_{toDate:ddMMyyyy}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi tạo báo cáo: {ex.Message}");
            }
        }
    }

    public class ExportReportRequest
    {
        public string Period { get; set; } = "today";
    }


}

    
