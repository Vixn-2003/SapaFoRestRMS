using BusinessAccessLayer.DTOs.Inventory;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.DTOs.Inventory;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.ComponentModel;


namespace WebSapaForestForStaff.Controllers
{
    public class MainImportInventoryController : Controller
    {
        private readonly HttpClient _httpClient;

        public MainImportInventoryController(HttpClient httpClient)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:7096/")
            };
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Gọi API với endpoint đúng
                var response = await _httpClient.GetAsync("api/PurchaseOrder");


                // Kiểm tra status code
                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage = $"Lỗi API: {response.StatusCode}";
                    return View("~/Views/Menu/MainImportInventory.cshtml", new List<PurchaseOrderDTO>());
                }

                // Đọc content
                var jsonData = await response.Content.ReadAsStringAsync();

                // Debug - log ra để kiểm tra
                Console.WriteLine("API Response: " + jsonData);


                // Parse JSON
                var purchaseList = JsonConvert.DeserializeObject<List<PurchaseOrderDTO>>(jsonData);

                // Kiểm tra null
                if (purchaseList == null)
                {
                    purchaseList = new List<PurchaseOrderDTO>();
                }

                return View("~/Views/Menu/MainImportInventory.cshtml", purchaseList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                ViewBag.ErrorMessage = ex.Message;
                return View("~/Views/Menu/MainImportInventory.cshtml", new List<PurchaseOrderDTO>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport([FromBody] ReportRequest request)
        {
            try
            {
                // ✅ THÊM: Validation
                if (request == null)
                {
                    return BadRequest(new { message = "Request không hợp lệ" });
                }

                if (request.Orders == null || request.Orders.Count == 0)
                {
                    return BadRequest(new { message = "Không có đơn hàng nào" });
                }

                // ✅ THÊM: Log để debug
                Console.WriteLine($"📥 Received {request.Orders.Count} orders");
                Console.WriteLine($"📅 Date range: {request.DateFrom} - {request.DateTo}");

                // Cấu hình license QuestPDF
                QuestPDF.Settings.License = LicenseType.Community;

                // Tạo PDF
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(12));

                        // Header
                        page.Header().Element(ComposeHeader);

                        // Content
                        page.Content().Element(container => ComposeContent(container, request));

                        // Footer
                        page.Footer().AlignCenter().Text(text =>
                        {
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                    });
                });

                // Generate PDF
                var pdfBytes = document.GeneratePdf();

                return File(pdfBytes, "application/pdf", $"BaoCaoNhapHang_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                // ✅ THÊM: Log chi tiết lỗi
                Console.WriteLine($"❌ Error in GenerateReport: {ex.Message}");
                Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                return BadRequest(new { message = ex.Message, detail = ex.StackTrace });
            }
        }

        private void ComposeHeader(QuestPDF.Infrastructure.IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("BÁO CÁO NHẬP HÀNG")
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    column.Item().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(10);
                });
            });
        }

        private void ComposeContent(QuestPDF.Infrastructure.IContainer container, ReportRequest request)
        {
            container.Column(column =>
            {
                // Thông tin khoảng thời gian
                column.Item().PaddingVertical(10).Row(row =>
                {
                    row.RelativeItem().Text($"Khoảng thời gian: {request.PeriodText}")
                        .FontSize(14)
                        .Bold();
                });

                // Thống kê tổng quan
                column.Item().PaddingBottom(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Tổng số đơn: {request.Orders.Count}")
                            .FontSize(12);

                        var totalValue = request.Orders.Sum(o => o.PurchaseOrderDetails?.Sum(d => d.Subtotal) ?? 0);
                        col.Item().Text($"Tổng giá trị: {totalValue:N0} VNĐ")
                            .FontSize(12);
                    });
                });

                // Bảng danh sách đơn hàng
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(50);  // STT
                        columns.RelativeColumn(2);   // Mã đơn
                        columns.RelativeColumn(3);   // Nhà cung cấp
                        columns.RelativeColumn(2);   // Ngày nhập
                        columns.RelativeColumn(2);   // Giá trị
                        columns.RelativeColumn(2);   // Trạng thái
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("STT").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Mã đơn").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Nhà cung cấp").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Ngày nhập").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Giá trị").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Trạng thái").Bold();
                    });

                    // Rows
                    int index = 1;
                    foreach (var order in request.Orders)
                    {
                        var total = order.PurchaseOrderDetails?.Sum(d => d.Subtotal) ?? 0;

                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(index.ToString());
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(order.PurchaseOrderId);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(order.Supplier?.Name ?? "N/A");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(order.OrderDate?.ToString("dd/MM/yyyy") ?? "-");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text($"{total:N0}");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(GetStatusText(order.Status));

                        index++;
                    }
                });
            });
        }

        private string GetStatusText(string status)
        {
            return status?.ToLower() switch
            {
                "pending" => "Chờ duyệt",
                "processing" => "Đang xử lý",
                "completed" => "Hoàn thành",
                "cancelled" => "Từ chối",
                _ => status ?? "N/A"
            };
        }

        // DTO class
        public class ReportRequest
        {
            public List<PurchaseOrderDTO> Orders { get; set; }
            public DateTime DateFrom { get; set; }
            public DateTime DateTo { get; set; }
            public string PeriodText { get; set; }
        }

    }
}
