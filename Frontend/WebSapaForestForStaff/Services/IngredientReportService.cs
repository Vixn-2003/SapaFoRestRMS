using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using WebSapaForestForStaff.DTOs.Inventory;

namespace WebSapaForestForStaff.Services
{
    public class IngredientReportService
    {
        public byte[] GenerateIngredientReport(IngredientReportDTO reportData)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 25, 25, 30, 30);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);

                document.Open();

                // Font tiếng Việt
                string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "Arial.ttf");
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                Font titleFont = new Font(baseFont, 18, Font.BOLD);
                Font headerFont = new Font(baseFont, 14, Font.BOLD);
                Font subHeaderFont = new Font(baseFont, 12, Font.BOLD);
                Font normalFont = new Font(baseFont, 10, Font.NORMAL);
                Font boldFont = new Font(baseFont, 10, Font.BOLD);
                Font smallFont = new Font(baseFont, 8, Font.NORMAL);

                // ===== HEADER =====
                Paragraph title = new Paragraph("BÁO CÁO QUẢN LÝ MẶT HÀNG", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 10;
                document.Add(title);

                Paragraph period = new Paragraph($"Kỳ báo cáo: {reportData.FromDate:dd/MM/yyyy} - {reportData.ToDate:dd/MM/yyyy}", normalFont);
                period.Alignment = Element.ALIGN_CENTER;
                period.SpacingAfter = 5;
                document.Add(period);

                Paragraph creator = new Paragraph($"Người lập báo cáo: {reportData.CreatedBy}", normalFont);
                creator.Alignment = Element.ALIGN_CENTER;
                creator.SpacingAfter = 5;
                document.Add(creator);

                Paragraph createDate = new Paragraph($"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}", normalFont);
                createDate.Alignment = Element.ALIGN_CENTER;
                createDate.SpacingAfter = 20;
                document.Add(createDate);

                // ===== I. TỔNG QUAN =====
                Paragraph section1 = new Paragraph("I. TỔNG QUAN", headerFont);
                section1.SpacingAfter = 10;
                document.Add(section1);

                PdfPTable overviewTable = new PdfPTable(2);
                overviewTable.WidthPercentage = 100;
                overviewTable.SetWidths(new float[] { 3f, 1f });
                overviewTable.SpacingAfter = 15;

                AddCell(overviewTable, "Tổng số loại mặt hàng đang quản lý:", boldFont);
                AddCell(overviewTable, $"{reportData.TotalIngredients} loại", normalFont);

                AddCell(overviewTable, "Tổng số lô hàng:", boldFont);
                AddCell(overviewTable, $"{reportData.TotalBatches} lô", normalFont);

                AddCell(overviewTable, "Số kho đang quản lý:", boldFont);
                AddCell(overviewTable, $"{reportData.TotalWarehouses} kho", normalFont);

                document.Add(overviewTable);

                // ===== II. CẢNH BÁO QUAN TRỌNG =====
                Paragraph section2 = new Paragraph("II. CẢNH BÁO QUAN TRỌNG", headerFont);
                section2.SpacingAfter = 10;
                document.Add(section2);

                // Tình trạng tồn kho
                Paragraph quantityTitle = new Paragraph("Tình trạng tồn kho:", subHeaderFont);
                quantityTitle.SpacingAfter = 5;
                document.Add(quantityTitle);

                PdfPTable quantityTable = new PdfPTable(2);
                quantityTable.WidthPercentage = 100;
                quantityTable.SetWidths(new float[] { 3f, 1f });
                quantityTable.SpacingAfter = 10;

                AddAlertCell(quantityTable, "Hết hàng:", reportData.OutOfStockCount, new BaseColor(239, 68, 68), boldFont, normalFont);
                AddAlertCell(quantityTable, "Sắp hết hàng (< 50% tối thiểu):", reportData.LowStockCount, new BaseColor(249, 115, 22), boldFont, normalFont);
                AddAlertCell(quantityTable, "Dưới tối thiểu:", reportData.BelowReorderCount, new BaseColor(245, 158, 11), boldFont, normalFont);
                AddAlertCell(quantityTable, "Cần nhập gấp (có lô hết hạn):", reportData.UrgentRestockCount, new BaseColor(139, 92, 246), boldFont, normalFont);

                document.Add(quantityTable);

                // Chất lượng mặt hàng
                Paragraph qualityTitle = new Paragraph("Chất lượng mặt hàng:", subHeaderFont);
                qualityTitle.SpacingAfter = 5;
                document.Add(qualityTitle);

                PdfPTable qualityTable = new PdfPTable(2);
                qualityTable.WidthPercentage = 100;
                qualityTable.SetWidths(new float[] { 3f, 1f });
                qualityTable.SpacingAfter = 15;

                AddAlertCell(qualityTable, "Lô hết hạn:", reportData.ExpiredBatchCount, new BaseColor(239, 68, 68), boldFont, normalFont);
                AddAlertCell(qualityTable, "Lô sắp hết hạn:", reportData.ExpiringSoonBatchCount, new BaseColor(245, 158, 11), boldFont, normalFont);

                document.Add(qualityTable);

                // ===== III. DANH SÁCH MẶT HÀNG CẦN NHẬP NGAY =====
                document.NewPage();
                Paragraph section3 = new Paragraph("III. DANH SÁCH MẶT HÀNG CẦN NHẬP NGAY", headerFont);
                section3.SpacingAfter = 10;
                document.Add(section3);

                // Ưu tiên 1: Hết hàng
                if (reportData.OutOfStockItems.Any())
                {
                    Paragraph priority1 = new Paragraph($"Ưu tiên 1 - HẾT HÀNG ({reportData.OutOfStockItems.Count} mặt hàng):", subHeaderFont);
                    priority1.SpacingAfter = 5;
                    document.Add(priority1);

                    PdfPTable outOfStockTable = new PdfPTable(5);
                    outOfStockTable.WidthPercentage = 100;
                    outOfStockTable.SetWidths(new float[] { 0.5f, 1.5f, 3f, 1f, 1f });
                    outOfStockTable.SpacingAfter = 10;

                    AddHeaderCell(outOfStockTable, "STT", boldFont);
                    AddHeaderCell(outOfStockTable, "Mã MH", boldFont);
                    AddHeaderCell(outOfStockTable, "Tên mặt hàng", boldFont);
                    AddHeaderCell(outOfStockTable, "ĐVT", boldFont);
                    AddHeaderCell(outOfStockTable, "SL tồn", boldFont);

                    int idx = 1;
                    foreach (var item in reportData.OutOfStockItems)
                    {
                        AddCell(outOfStockTable, idx.ToString(), normalFont);
                        AddCell(outOfStockTable, item.IngredientCode, normalFont);
                        AddCell(outOfStockTable, item.IngredientName, normalFont);
                        AddCell(outOfStockTable, item.Unit, normalFont);
                        AddCell(outOfStockTable, "0", normalFont);
                        idx++;
                    }

                    document.Add(outOfStockTable);
                }

                // Ưu tiên 2: Sắp hết hàng
                if (reportData.LowStockItems.Any())
                {
                    Paragraph priority2 = new Paragraph($"Ưu tiên 2 - SẮP HẾT HÀNG ({reportData.LowStockItems.Count} mặt hàng):", subHeaderFont);
                    priority2.SpacingAfter = 5;
                    document.Add(priority2);

                    PdfPTable lowStockTable = new PdfPTable(6);
                    lowStockTable.WidthPercentage = 100;
                    lowStockTable.SetWidths(new float[] { 0.5f, 1.5f, 3f, 1f, 1.5f, 1.5f });
                    lowStockTable.SpacingAfter = 10;

                    AddHeaderCell(lowStockTable, "STT", boldFont);
                    AddHeaderCell(lowStockTable, "Mã MH", boldFont);
                    AddHeaderCell(lowStockTable, "Tên mặt hàng", boldFont);
                    AddHeaderCell(lowStockTable, "ĐVT", boldFont);
                    AddHeaderCell(lowStockTable, "SL tồn", boldFont);
                    AddHeaderCell(lowStockTable, "Tối thiểu", boldFont);

                    int idx2 = 1;
                    foreach (var item in reportData.LowStockItems)
                    {
                        AddCell(lowStockTable, idx2.ToString(), normalFont);
                        AddCell(lowStockTable, item.IngredientCode, normalFont);
                        AddCell(lowStockTable, item.IngredientName, normalFont);
                        AddCell(lowStockTable, item.Unit, normalFont);
                        AddCell(lowStockTable, item.CurrentQuantity.ToString("N2"), normalFont);
                        AddCell(lowStockTable, item.ReorderLevel?.ToString("N2") ?? "-", normalFont);
                        idx2++;
                    }

                    document.Add(lowStockTable);
                }

                // Ưu tiên 3: Cần nhập gấp
                if (reportData.UrgentRestockItems.Any())
                {
                    Paragraph priority3 = new Paragraph($"Ưu tiên 3 - CẦN NHẬP GẤP (có lô hết hạn) ({reportData.UrgentRestockItems.Count} mặt hàng):", subHeaderFont);
                    priority3.SpacingAfter = 5;
                    document.Add(priority3);

                    PdfPTable urgentTable = new PdfPTable(7);
                    urgentTable.WidthPercentage = 100;
                    urgentTable.SetWidths(new float[] { 0.5f, 1.5f, 2.5f, 1f, 1.5f, 1.5f, 1.5f });
                    urgentTable.SpacingAfter = 10;

                    AddHeaderCell(urgentTable, "STT", boldFont);
                    AddHeaderCell(urgentTable, "Mã MH", boldFont);
                    AddHeaderCell(urgentTable, "Tên mặt hàng", boldFont);
                    AddHeaderCell(urgentTable, "ĐVT", boldFont);
                    AddHeaderCell(urgentTable, "SL hiện tại", boldFont);
                    AddHeaderCell(urgentTable, "Sau trừ hết hạn", boldFont);
                    AddHeaderCell(urgentTable, "Tối thiểu", boldFont);

                    int idx3 = 1;
                    foreach (var item in reportData.UrgentRestockItems)
                    {
                        AddCell(urgentTable, idx3.ToString(), normalFont);
                        AddCell(urgentTable, item.IngredientCode, normalFont);
                        AddCell(urgentTable, item.IngredientName, normalFont);
                        AddCell(urgentTable, item.Unit, normalFont);
                        AddCell(urgentTable, item.CurrentQuantity.ToString("N2"), normalFont);
                        AddCell(urgentTable, item.QuantityExcludingExpired?.ToString("N2") ?? "-", normalFont);
                        AddCell(urgentTable, item.ReorderLevel?.ToString("N2") ?? "-", normalFont);
                        idx3++;
                    }

                    document.Add(urgentTable);

                    Paragraph note = new Paragraph("Lưu ý: Các mặt hàng có lô hết hạn cần xử lý và nhập bổ sung ngay để đảm bảo đủ số lượng tối thiểu cho hoạt động.", smallFont);
                    note.SpacingAfter = 15;
                    document.Add(note);
                }

                // ===== V. DANH SÁCH LÔ HẾT HẠN HOẶC SẮP HẾT HẠN =====
                document.NewPage();
                Paragraph section5 = new Paragraph("V. DANH SÁCH LÔ HẾT HẠN HOẶC SẮP HẾT HẠN", headerFont);
                section5.SpacingAfter = 10;
                document.Add(section5);

                // Lô hết hạn
                if (reportData.ExpiredBatches.Any())
                {
                    Paragraph expiredTitle = new Paragraph($"Lô hết hạn ({reportData.ExpiredBatches.Count} lô):", subHeaderFont);
                    expiredTitle.SpacingAfter = 5;
                    document.Add(expiredTitle);

                    PdfPTable expiredTable = new PdfPTable(7);
                    expiredTable.WidthPercentage = 100;
                    expiredTable.SetWidths(new float[] { 0.5f, 1.5f, 2f, 1.5f, 1.5f, 1.5f, 2f });
                    expiredTable.SpacingAfter = 10;

                    AddHeaderCell(expiredTable, "STT", boldFont);
                    AddHeaderCell(expiredTable, "Mã lô", boldFont);
                    AddHeaderCell(expiredTable, "Mặt hàng", boldFont);
                    AddHeaderCell(expiredTable, "SL còn", boldFont);
                    AddHeaderCell(expiredTable, "Ngày nhập", boldFont);
                    AddHeaderCell(expiredTable, "Hạn SD", boldFont);
                    AddHeaderCell(expiredTable, "Kho", boldFont);

                    int idxExp = 1;
                    foreach (var batch in reportData.ExpiredBatches)
                    {
                        AddCell(expiredTable, idxExp.ToString(), normalFont);
                        AddCell(expiredTable, batch.BatchCode, normalFont);
                        AddCell(expiredTable, batch.IngredientName, normalFont);
                        AddCell(expiredTable, $"{batch.QuantityRemaining:N2} {batch.Unit}", normalFont);
                        AddCell(expiredTable, batch.ImportDate?.ToString("dd/MM/yyyy") ?? "-", normalFont);
                        AddCell(expiredTable, batch.ExpiryDate?.ToString("dd/MM/yyyy") ?? "-", normalFont);
                        AddCell(expiredTable, batch.WarehouseName, normalFont);
                        idxExp++;
                    }

                    document.Add(expiredTable);

                    Paragraph expiredNote = new Paragraph("Khuyến nghị: Ngừng sử dụng các lô này và xử lý theo quy định.", new Font(baseFont, 10, Font.ITALIC));
                    expiredNote.SpacingAfter = 15;
                    document.Add(expiredNote);
                }

                // Lô sắp hết hạn
                if (reportData.ExpiringSoonBatches.Any())
                {
                    Paragraph expiringTitle = new Paragraph($"Lô sắp hết hạn (còn < 7 ngày) ({reportData.ExpiringSoonBatches.Count} lô):", subHeaderFont);
                    expiringTitle.SpacingAfter = 5;
                    document.Add(expiringTitle);

                    PdfPTable expiringTable = new PdfPTable(7);
                    expiringTable.WidthPercentage = 100;
                    expiringTable.SetWidths(new float[] { 0.5f, 1.5f, 2f, 1.5f, 1.5f, 1.5f, 2f });
                    expiringTable.SpacingAfter = 10;

                    AddHeaderCell(expiringTable, "STT", boldFont);
                    AddHeaderCell(expiringTable, "Mã lô", boldFont);
                    AddHeaderCell(expiringTable, "Mặt hàng", boldFont);
                    AddHeaderCell(expiringTable, "SL còn", boldFont);
                    AddHeaderCell(expiringTable, "Ngày nhập", boldFont);
                    AddHeaderCell(expiringTable, "Hạn SD", boldFont);
                    AddHeaderCell(expiringTable, "Kho", boldFont);

                    int idxExpiring = 1;
                    foreach (var batch in reportData.ExpiringSoonBatches)
                    {
                        AddCell(expiringTable, idxExpiring.ToString(), normalFont);
                        AddCell(expiringTable, batch.BatchCode, normalFont);
                        AddCell(expiringTable, batch.IngredientName, normalFont);
                        AddCell(expiringTable, $"{batch.QuantityRemaining:N2} {batch.Unit}", normalFont);
                        AddCell(expiringTable, batch.ImportDate?.ToString("dd/MM/yyyy") ?? "-", normalFont);
                        AddCell(expiringTable, batch.ExpiryDate?.ToString("dd/MM/yyyy") ?? "-", normalFont);
                        AddCell(expiringTable, batch.WarehouseName, normalFont);
                        idxExpiring++;
                    }

                    document.Add(expiringTable);

                    Paragraph expiringNote = new Paragraph("Khuyến nghị: Ưu tiên sử dụng các lô này trước để giảm tổn thất.", new Font(baseFont, 10, Font.ITALIC));
                    expiringNote.SpacingAfter = 15;
                    document.Add(expiringNote);
                }

                // ===== VI. CHI TIẾT TOÀN BỘ MẶT HÀNG =====
                document.NewPage();
                Paragraph section6 = new Paragraph("VI. CHI TIẾT TOÀN BỘ MẶT HÀNG", headerFont);
                section6.SpacingAfter = 10;
                document.Add(section6);

                if (reportData.AllIngredients.Any())
                {
                    PdfPTable allTable = new PdfPTable(6);
                    allTable.WidthPercentage = 100;
                    allTable.SetWidths(new float[] { 0.5f, 1.5f, 3f, 1f, 1.5f, 1.5f });

                    AddHeaderCell(allTable, "STT", boldFont);
                    AddHeaderCell(allTable, "Mã MH", boldFont);
                    AddHeaderCell(allTable, "Tên mặt hàng", boldFont);
                    AddHeaderCell(allTable, "ĐVT", boldFont);
                    AddHeaderCell(allTable, "SL tồn", boldFont);
                    AddHeaderCell(allTable, "Trạng thái", boldFont);

                    int idxAll = 1;
                    foreach (var item in reportData.AllIngredients)
                    {
                        AddCell(allTable, idxAll.ToString(), normalFont);
                        AddCell(allTable, item.IngredientCode, normalFont);
                        AddCell(allTable, item.IngredientName, normalFont);
                        AddCell(allTable, item.Unit, normalFont);
                        AddCell(allTable, item.TotalQuantity.ToString("N2"), normalFont);
                        AddCell(allTable, item.StatusText, normalFont);
                        idxAll++;
                    }

                    document.Add(allTable);
                }

              

                // ===== FOOTER - Chữ ký =====
                document.NewPage();

                Paragraph signatureTitle = new Paragraph("\n\n\nXÁC NHẬN", headerFont);
                signatureTitle.Alignment = Element.ALIGN_CENTER;
                signatureTitle.SpacingAfter = 30;
                document.Add(signatureTitle);

                PdfPTable signatureTable = new PdfPTable(2);
                signatureTable.WidthPercentage = 100;
                signatureTable.SetWidths(new float[] { 1f, 1f });

                PdfPCell cell1 = new PdfPCell(new Phrase("Người lập báo cáo\n\n\n\n\n\n" + reportData.CreatedBy, normalFont));
                cell1.Border = Rectangle.NO_BORDER;
                cell1.HorizontalAlignment = Element.ALIGN_CENTER;
                signatureTable.AddCell(cell1);

                PdfPCell cell2 = new PdfPCell(new Phrase("Quản lý\n\n\n\n\n\n________________", normalFont));
                cell2.Border = Rectangle.NO_BORDER;
                cell2.HorizontalAlignment = Element.ALIGN_CENTER;
                signatureTable.AddCell(cell2);

                document.Add(signatureTable);

                document.Close();
                writer.Close();

                return ms.ToArray();
            }
        }

        private void AddCell(PdfPTable table, string text, Font font)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.Padding = 5;
            table.AddCell(cell);
        }

        private void AddHeaderCell(PdfPTable table, string text, Font font)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.BackgroundColor = new BaseColor(249, 250, 251);
            cell.Padding = 8;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            table.AddCell(cell);
        }

        private void AddAlertCell(PdfPTable table, string label, int count, BaseColor color, Font labelFont, Font valueFont)
        {
            PdfPCell labelCell = new PdfPCell(new Phrase(label, labelFont));
            labelCell.Padding = 8;
            labelCell.Border = Rectangle.NO_BORDER;
            table.AddCell(labelCell);

            PdfPCell valueCell = new PdfPCell(new Phrase($"{count} mặt hàng", valueFont));
            valueCell.Padding = 8;
            valueCell.BackgroundColor = color;
            valueCell.HorizontalAlignment = Element.ALIGN_CENTER;
            valueCell.Border = Rectangle.NO_BORDER;
            table.AddCell(valueCell);
        }
    }
}