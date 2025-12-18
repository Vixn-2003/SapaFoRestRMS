using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json;
using WebSapaForestForStaff.DTOs.Inventory;
using System.Globalization;

namespace WebSapaForestForStaff.Services
{
    public class ReportService : IReportService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReportService> _logger;
        private readonly IWebHostEnvironment _env;

        // ✅ FONT PATH - Quan trọng cho Tiếng Việt
        private string _fontPath;

        public ReportService(
            HttpClient httpClient,
            ILogger<ReportService> logger,
            IWebHostEnvironment env)
        {
            _httpClient = httpClient;
            _logger = logger;
            _env = env;
            _httpClient.BaseAddress = new Uri("https://localhost:7096/");

            // ✅ Setup font Tiếng Việt
            _fontPath = Path.Combine(_env.WebRootPath, "fonts", "arial.ttf");

            if (!File.Exists(_fontPath))
            {
                _fontPath = @"C:\Windows\Fonts\arial.ttf";
            }
        }

        public async Task<byte[]> GenerateAuditReportPdfAsync(AuditReportRequest request)
        {
            try
            {
                _logger.LogInformation($"Generating audit report from {request.DateFrom:yyyy-MM-dd} to {request.DateTo:yyyy-MM-dd}");

                // 1. Lấy dữ liệu
                var audits = await FetchAuditDataAsync(request);

                if (audits.Count == 0)
                {
                    throw new Exception("Không có dữ liệu kiểm kê trong kỳ báo cáo này");
                }

                // 2. Tính toán thống kê
                var stats = CalculateStatistics(audits);

                // 3. Tạo PDF
                var pdfBytes = GeneratePdfDocument(request, audits, stats);

                _logger.LogInformation($"Generated PDF successfully with {audits.Count} audits");

                return pdfBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating audit report PDF");
                throw;
            }
        }

        // ✅ LẤY DỮ LIỆU TỪ API
        private async Task<List<AuditInventoryDTO>> FetchAuditDataAsync(AuditReportRequest request)
        {
            var response = await _httpClient.GetAsync("api/AuditInventory/GetAll");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Không thể tải dữ liệu kiểm kê từ API");
            }

            var json = await response.Content.ReadAsStringAsync();
            var allAudits = JsonConvert.DeserializeObject<List<AuditInventoryDTO>>(json)
                ?? new List<AuditInventoryDTO>();

            // Lọc theo điều kiện
            return allAudits.Where(a =>
                a.CreatedAt >= request.DateFrom &&
                a.CreatedAt <= request.DateTo &&
                (string.IsNullOrEmpty(request.Status) || a.AuditStatus == request.Status)
            ).OrderByDescending(a => a.CreatedAt).ToList();
        }

        // ✅ TÍNH TOÁN THỐNG KÊ (ĐƠN GIẢN HƠN)
        private ReportStatistics CalculateStatistics(List<AuditInventoryDTO> audits)
        {
            return new ReportStatistics
            {
                TotalAudits = audits.Count,
                CompletedCount = audits.Count(a => a.AuditStatus == "completed"),
                ProcessingCount = audits.Count(a => a.AuditStatus == "processing"),
                CancelledCount = audits.Count(a => a.AuditStatus == "cancelled")
            };
        }

        // ✅ TẠO PDF DOCUMENT (KHÔNG MÀU, ĐƠN GIẢN)
        private byte[] GeneratePdfDocument(AuditReportRequest request, List<AuditInventoryDTO> audits, ReportStatistics stats)
        {
            using (var memoryStream = new MemoryStream())
            {
                // Tạo document A4 ngang
                var document = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
                var writer = PdfWriter.GetInstance(document, memoryStream);

                document.Open();

                // Setup fonts (không màu)
                var baseFont = BaseFont.CreateFont(_fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                var titleFont = new Font(baseFont, 18, Font.BOLD, BaseColor.Black);
                var headerFont = new Font(baseFont, 12, Font.BOLD, BaseColor.Black);
                var normalFont = new Font(baseFont, 10, Font.NORMAL, BaseColor.Black);
                var smallFont = new Font(baseFont, 9, Font.NORMAL, BaseColor.Black);
                var boldFont = new Font(baseFont, 10, Font.BOLD, BaseColor.Black);

                // ============ HEADER ============
                var titleParagraph = new Paragraph("BÁO CÁO KIỂM KÊ KHO", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 10
                };
                document.Add(titleParagraph);

                // Thông tin báo cáo
                var infoParagraph = new Paragraph();
                infoParagraph.Add(new Chunk($"Kỳ báo cáo: {request.DateFrom:dd/MM/yyyy} - {request.DateTo:dd/MM/yyyy}\n", normalFont));
                infoParagraph.Add(new Chunk($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}\n", normalFont));
                infoParagraph.Add(new Chunk($"Người xuất: {request.ReporterName} - {request.ReporterPosition}\n", normalFont));
                infoParagraph.SpacingAfter = 15;
                document.Add(infoParagraph);

                // Line separator
                document.Add(new Paragraph(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(1f, 100f, BaseColor.Black, Element.ALIGN_CENTER, -5))));
                document.Add(new Paragraph(" ") { SpacingAfter = 10 });

                // ============ THỐNG KÊ (KHÔNG MÀU, ĐƠN GIẢN) ============
                var statsTitle = new Paragraph("TỔNG QUAN", headerFont) { SpacingAfter = 10 };
                document.Add(statsTitle);

                var statsTable = new PdfPTable(4) { WidthPercentage = 100, SpacingAfter = 20 };
                statsTable.SetWidths(new float[] { 1, 1, 1, 1 });

                // Stats cells (không màu nền)
                AddStatsCell(statsTable, "Tổng số đơn", stats.TotalAudits.ToString(), baseFont);
                AddStatsCell(statsTable, "Đã xác nhận", stats.CompletedCount.ToString(), baseFont);
                AddStatsCell(statsTable, "Đang xử lý", stats.ProcessingCount.ToString(), baseFont);
                AddStatsCell(statsTable, "Đã từ chối", stats.CancelledCount.ToString(), baseFont);

                document.Add(statsTable);

                // ============ BẢNG CHI TIẾT (KHÔNG MÀU) ============
                var detailTitle = new Paragraph("CHI TIẾT CÁC ĐỢT KIỂM KÊ", headerFont) { SpacingAfter = 10 };
                document.Add(detailTitle);

                var table = new PdfPTable(8) { WidthPercentage = 100 };
                table.SetWidths(new float[] { 1f, 1f, 2f, 1f, 1f, 2f, 1.5f, 1f });

                // Header (không màu nền)
                AddHeaderCell(table, "Mã đơn", boldFont);
                AddHeaderCell(table, "Mã lô", boldFont);
                AddHeaderCell(table, "Mặt hàng", boldFont);
                AddHeaderCell(table, "SL gốc", boldFont);
                AddHeaderCell(table, "Điều chỉnh", boldFont);
                AddHeaderCell(table, "Lý do", boldFont);
                AddHeaderCell(table, "Thời gian tạo", boldFont);
                AddHeaderCell(table, "Trạng thái", boldFont);

                // Data rows (không màu, không có màu chữ)
                foreach (var audit in audits)
                {
                    AddDataCell(table, audit.AuditId, smallFont);
                    AddDataCell(table, audit.PurchaseOrderId, smallFont);
                    AddDataCell(table, audit.IngredientName, smallFont);
                    AddDataCell(table, $"{audit.OriginalQuantity:N2} {audit.Unit}", smallFont);

                    // Điều chỉnh (không màu chữ)
                    var adjustmentText = $"{(audit.IsAddition ? "+" : "-")}{audit.AdjustmentQuantity:N2} {audit.Unit}";
                    AddDataCell(table, adjustmentText, smallFont);

                    AddDataCell(table, audit.Reason, new Font(baseFont, 8, Font.NORMAL, BaseColor.Black));

                    // ✅ THỜI GIAN TẠO (thay vì số lượng mới)
                    AddDataCell(table, audit.CreatedAt.ToString("dd/MM/yyyy HH:mm"), smallFont);

                    // Trạng thái (không màu chữ)
                    var statusText = audit.AuditStatus == "completed" ? "Đã xác nhận" :
                                   audit.AuditStatus == "processing" ? "Đang xử lý" : "Đã từ chối";
                    AddDataCell(table, statusText, smallFont);
                }

                document.Add(table);

                // ============ PHẦN CHỮ KÝ ============
                document.Add(new Paragraph(" ") { SpacingBefore = 30 });

                // Tạo bảng 2 cột cho chữ ký
                var signatureTable = new PdfPTable(2) { WidthPercentage = 100 };
                signatureTable.SetWidths(new float[] { 1f, 1f });

                // Cột trái - Nhân viên kho
                var warehouseStaffCell = new PdfPCell();
                warehouseStaffCell.Border = Rectangle.NO_BORDER;
                warehouseStaffCell.HorizontalAlignment = Element.ALIGN_CENTER;
                warehouseStaffCell.PaddingTop = 10;

                var warehouseStaffPara = new Paragraph();
                warehouseStaffPara.Add(new Chunk("NHÂN VIÊN KHO\n", new Font(baseFont, 11, Font.BOLD, BaseColor.Black)));
                warehouseStaffPara.Add(new Chunk("(Ký, ghi rõ họ tên)\n\n\n\n\n", new Font(baseFont, 9, Font.ITALIC, BaseColor.Black)));
                warehouseStaffPara.Add(new Chunk("_______________________", normalFont));
                warehouseStaffPara.Alignment = Element.ALIGN_CENTER;
                warehouseStaffCell.AddElement(warehouseStaffPara);

                // Cột phải - Quản lý
                var managerCell = new PdfPCell();
                managerCell.Border = Rectangle.NO_BORDER;
                managerCell.HorizontalAlignment = Element.ALIGN_CENTER;
                managerCell.PaddingTop = 10;

                var managerPara = new Paragraph();
                managerPara.Add(new Chunk("QUẢN LÝ\n", new Font(baseFont, 11, Font.BOLD, BaseColor.Black)));
                managerPara.Add(new Chunk("(Ký, ghi rõ họ tên)\n\n\n\n\n", new Font(baseFont, 9, Font.ITALIC, BaseColor.Black)));
                managerPara.Add(new Chunk("_______________________", normalFont));
                managerPara.Alignment = Element.ALIGN_CENTER;
                managerCell.AddElement(managerPara);

                signatureTable.AddCell(warehouseStaffCell);
                signatureTable.AddCell(managerCell);

                document.Add(signatureTable);

                // ============ FOOTER ============
                document.Add(new Paragraph(" ") { SpacingBefore = 20 });
                var footerLine = new Paragraph(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(0.5f, 100f, BaseColor.Gray, Element.ALIGN_CENTER, -5)));
                document.Add(footerLine);

                var footer = new Paragraph($"Báo cáo được tạo tự động bởi hệ thống quản lý kho - {DateTime.Now:dd/MM/yyyy HH:mm}",
                    new Font(baseFont, 8, Font.ITALIC, BaseColor.Black))
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 5
                };
                document.Add(footer);

                document.Close();
                writer.Close();

                return memoryStream.ToArray();
            }
        }

        // ✅ HELPER METHODS (ĐƠN GIẢN, KHÔNG MÀU)
        private void AddStatsCell(PdfPTable table, string label, string value, BaseFont baseFont)
        {
            var cell = new PdfPCell();
            cell.BackgroundColor = BaseColor.White; // Không màu nền
            cell.BorderWidth = 1;
            cell.BorderColor = BaseColor.Black;
            cell.Padding = 8;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;

            var labelPara = new Paragraph(label, new Font(baseFont, 8, Font.NORMAL, BaseColor.Black)) { Alignment = Element.ALIGN_CENTER };
            var valuePara = new Paragraph(value, new Font(baseFont, 16, Font.BOLD, BaseColor.Black)) { Alignment = Element.ALIGN_CENTER };

            cell.AddElement(labelPara);
            cell.AddElement(valuePara);

            table.AddCell(cell);
        }

        private void AddHeaderCell(PdfPTable table, string text, Font font)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                BackgroundColor = BaseColor.White, // Không màu nền
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Padding = 8,
                BorderWidth = 1,
                BorderColor = BaseColor.Black
            };
            table.AddCell(cell);
        }

        private void AddDataCell(PdfPTable table, string text, Font font)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                BackgroundColor = BaseColor.White, // Không màu nền
                HorizontalAlignment = Element.ALIGN_LEFT,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Padding = 5,
                BorderWidth = 0.5f,
                BorderColor = BaseColor.Black
            };
            table.AddCell(cell);
        }
    }

    // ✅ DTO CHO THỐNG KÊ (ĐƠN GIẢN HƠN)
    public class ReportStatistics
    {
        public int TotalAudits { get; set; }
        public int CompletedCount { get; set; }
        public int ProcessingCount { get; set; }
        public int CancelledCount { get; set; }
    }
}