using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using WebSapaForestForStaff.DTOs.Inventory;

namespace WebSapaForestForStaff.Services
{
    public class ExportReportService
    {
        public byte[] GenerateExportReport(ExportReportDTO reportData)
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
                Font normalFont = new Font(baseFont, 10, Font.NORMAL);
                Font boldFont = new Font(baseFont, 10, Font.BOLD);
                Font smallFont = new Font(baseFont, 8, Font.NORMAL);

                // HEADER
                Paragraph title = new Paragraph("BÁO CÁO XUẤT NGUYÊN LIỆU", titleFont);
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

                // I. TỔNG QUAN
                Paragraph section1 = new Paragraph("I. TỔNG QUAN", headerFont);
                section1.SpacingAfter = 10;
                document.Add(section1);

                PdfPTable overviewTable = new PdfPTable(2);
                overviewTable.WidthPercentage = 100;
                overviewTable.SpacingAfter = 15;

                AddCell(overviewTable, "Tổng số giao dịch xuất kho:", boldFont);
                AddCell(overviewTable, reportData.TotalTransactions.ToString(), normalFont);

                AddCell(overviewTable, "Số loại nguyên liệu xuất:", boldFont);
                AddCell(overviewTable, reportData.Transactions.Select(x => x.IngredientId).Distinct().Count().ToString(), normalFont);

                AddCell(overviewTable, "Số lượng xuất bất thường:", boldFont);
                AddCell(overviewTable, reportData.AbnormalExports.Count.ToString() + " loại", normalFont);

                document.Add(overviewTable);

                // II. TOP NGUYÊN LIỆU XUẤT
                if (reportData.TopIngredients.Any())
                {
                    Paragraph section2 = new Paragraph("II. TOP NGUYÊN LIỆU XUẤT NHIỀU NHẤT", headerFont);
                    section2.SpacingAfter = 10;
                    document.Add(section2);

                    PdfPTable topTable = new PdfPTable(3);
                    topTable.WidthPercentage = 100;
                    topTable.SetWidths(new float[] { 1f, 3f, 2f });
                    topTable.SpacingAfter = 15;

                    AddHeaderCell(topTable, "STT", boldFont);
                    AddHeaderCell(topTable, "Tên nguyên liệu", boldFont);
                    AddHeaderCell(topTable, "Số lượng", boldFont);

                    int idx = 1;
                    foreach (var item in reportData.TopIngredients)
                    {
                        AddCell(topTable, idx.ToString(), normalFont);
                        AddCell(topTable, item.IngredientName, normalFont);
                        AddCell(topTable, $"{item.TotalQuantity:N2} {item.UnitName}", normalFont);
                        idx++;
                    }

                    document.Add(topTable);
                }

                // III. XUẤT THEO KHO
                if (reportData.WarehouseStats.Any())
                {
                    Paragraph section3 = new Paragraph("III. XUẤT THEO KHO", headerFont);
                    section3.SpacingAfter = 10;
                    document.Add(section3);

                    PdfPTable warehouseTable = new PdfPTable(2);
                    warehouseTable.WidthPercentage = 100;
                    warehouseTable.SetWidths(new float[] { 3f, 2f });
                    warehouseTable.SpacingAfter = 15;

                    AddHeaderCell(warehouseTable, "Kho", boldFont);
                    AddHeaderCell(warehouseTable, "Số giao dịch", boldFont);

                    foreach (var item in reportData.WarehouseStats)
                    {
                        AddCell(warehouseTable, item.WarehouseName, normalFont);
                        AddCell(warehouseTable, item.TransactionCount.ToString(), normalFont);
                    }

                    document.Add(warehouseTable);
                }

                // IV. CẢNH BÁO XUẤT BẤT THƯỜNG
                if (reportData.AbnormalExports.Any())
                {
                    document.NewPage();

                    Paragraph section4 = new Paragraph("IV. CẢNH BÁO XUẤT BẤT THƯỜNG (Tăng >50%)", headerFont);
                    section4.SpacingAfter = 10;
                    document.Add(section4);

                    PdfPTable abnormalTable = new PdfPTable(4);
                    abnormalTable.WidthPercentage = 100;
                    abnormalTable.SetWidths(new float[] { 3f, 2f, 2f, 2f });
                    abnormalTable.SpacingAfter = 15;

                    AddHeaderCell(abnormalTable, "Nguyên liệu", boldFont);
                    AddHeaderCell(abnormalTable, "Hôm nay", boldFont);
                    AddHeaderCell(abnormalTable, "TB 7 ngày", boldFont);
                    AddHeaderCell(abnormalTable, "% Chênh lệch", boldFont);

                    foreach (var item in reportData.AbnormalExports)
                    {
                        AddCell(abnormalTable, item.IngredientName, normalFont);
                        AddCell(abnormalTable, $"{item.TodayQuantity:N2} {item.UnitName}", normalFont);
                        AddCell(abnormalTable, $"{item.AvgQuantity:N2} {item.UnitName}", normalFont);

                        PdfPCell percentCell = new PdfPCell(new Phrase($"+{item.PercentChange:N0}%", boldFont));
                        percentCell.BackgroundColor = new BaseColor(254, 226, 226); // Màu đỏ nhạt
                        percentCell.Padding = 5;
                        abnormalTable.AddCell(percentCell);
                    }

                    document.Add(abnormalTable);
                }

                // V. SO SÁNH TIÊU HAO
                if (reportData.Comparisons.Any())
                {
                    document.NewPage();

                    Paragraph section5 = new Paragraph("V. SO SÁNH TIÊU HAO", headerFont);
                    section5.SpacingAfter = 10;
                    document.Add(section5);

                    PdfPTable comparisonTable = new PdfPTable(4);
                    comparisonTable.WidthPercentage = 100;
                    comparisonTable.SetWidths(new float[] { 3f, 2f, 2f, 2f });
                    comparisonTable.SpacingAfter = 15;

                    AddHeaderCell(comparisonTable, "Nguyên liệu", boldFont);
                    AddHeaderCell(comparisonTable, "Hôm nay", boldFont);
                    AddHeaderCell(comparisonTable, "TB 7 ngày", boldFont);
                    AddHeaderCell(comparisonTable, "% Chênh lệch", boldFont);

                    foreach (var item in reportData.Comparisons)
                    {
                        AddCell(comparisonTable, item.IngredientName, normalFont);
                        AddCell(comparisonTable, $"{item.TodayQuantity:N2} {item.UnitName}", normalFont);
                        AddCell(comparisonTable, $"{item.AvgQuantity:N2} {item.UnitName}", normalFont);

                        string arrow = item.PercentChange > 0 ? "↑" : item.PercentChange < 0 ? "↓" : "→";
                        BaseColor bgColor = item.PercentChange > 20 ? new BaseColor(254, 226, 226) :
                                          item.PercentChange < -20 ? new BaseColor(209, 250, 229) :
                                          BaseColor.White;

                        PdfPCell percentCell = new PdfPCell(new Phrase($"{arrow} {item.PercentChange:N0}%", normalFont));
                        percentCell.BackgroundColor = bgColor;
                        percentCell.Padding = 5;
                        comparisonTable.AddCell(percentCell);
                    }

                    document.Add(comparisonTable);
                }

                // VI. CHI TIẾT GIAO DỊCH
                document.NewPage();

                Paragraph section6 = new Paragraph("VI. CHI TIẾT GIAO DỊCH XUẤT KHO", headerFont);
                section6.SpacingAfter = 10;
                document.Add(section6);

                PdfPTable detailTable = new PdfPTable(6);
                detailTable.WidthPercentage = 100;
                detailTable.SetWidths(new float[] { 3f, 2f, 2f, 2f, 2f, 2f });

                AddHeaderCell(detailTable, "Nguyên liệu", smallFont);
                AddHeaderCell(detailTable, "Số lượng", smallFont);
                AddHeaderCell(detailTable, "Thời gian", smallFont);
                AddHeaderCell(detailTable, "Kho", smallFont);
                AddHeaderCell(detailTable, "Lô hàng", smallFont);
                AddHeaderCell(detailTable, "NCC", smallFont);

                foreach (var item in reportData.Transactions.OrderByDescending(x => x.TransactionDate))
                {
                    AddCell(detailTable, $"{item.IngredientName}\n({item.IngredientCode})", smallFont);
                    AddCell(detailTable, $"{item.Quantity:N2} {item.UnitName}", smallFont);
                    AddCell(detailTable, item.TransactionDate?.ToString("dd/MM/yyyy HH:mm") ?? "-", smallFont);
                    AddCell(detailTable, item.WarehouseName, smallFont);
                    AddCell(detailTable, item.PurchaseOrderId ?? $"#LOT-{item.BatchId}", smallFont);
                    AddCell(detailTable, item.SupplierName ?? "-", smallFont);
                }

                document.Add(detailTable);

                // FOOTER - Chữ ký
                document.NewPage();

                Paragraph signatureTitle = new Paragraph("\n\n\nXÁC NHẬN", headerFont);
                signatureTitle.Alignment = Element.ALIGN_CENTER;
                signatureTitle.SpacingAfter = 30;
                document.Add(signatureTitle);

                PdfPTable signatureTable = new PdfPTable(3);
                signatureTable.WidthPercentage = 100;
                signatureTable.SetWidths(new float[] { 1f, 1f, 1f });

                PdfPCell cell1 = new PdfPCell(new Phrase("Người lập báo cáo\n\n\n\n\n\n" + reportData.CreatedBy, normalFont));
                cell1.Border = Rectangle.NO_BORDER;
                cell1.HorizontalAlignment = Element.ALIGN_CENTER;
                signatureTable.AddCell(cell1);

                PdfPCell cell3 = new PdfPCell(new Phrase("Quản lý\n\n\n\n\n\n________________", normalFont));
                cell3.Border = Rectangle.NO_BORDER;
                cell3.HorizontalAlignment = Element.ALIGN_CENTER;
                signatureTable.AddCell(cell3);

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
    }
}