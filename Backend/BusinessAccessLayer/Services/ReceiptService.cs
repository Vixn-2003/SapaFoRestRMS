using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.Extensions.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BusinessAccessLayer.Services;

/// <summary>
/// Service for generating PDF receipts for paid orders
/// </summary>
public class ReceiptService : IReceiptService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly string _webRootPath;

    public ReceiptService(IUnitOfWork unitOfWork, string webRootPath)
    {
        _unitOfWork = unitOfWork;
        _webRootPath = webRootPath ?? throw new ArgumentNullException(nameof(webRootPath));
        
        // Set QuestPDF license (free for non-commercial use)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<string> GenerateReceiptPdfAsync(int orderId, CancellationToken ct = default)
    {
        // Get order with all related data (includes OrderDetails, MenuItem, Transactions, etc.)
        var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(orderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {orderId}");
        }

        // Check if order is paid
        if (order.Status != "Paid" && order.Status != "PAID")
        {
            throw new InvalidOperationException($"Đơn hàng chưa được thanh toán. Trạng thái hiện tại: {order.Status}");
        }

        // Generate order code
        var orderCode = $"RMS{orderId:D6}";

        // Calculate amounts
        var subtotal = order.OrderDetails?.Sum(od => od.UnitPrice * od.Quantity) ?? 0;
        var vatAmount = subtotal * 0.1m; // 10% VAT
        var serviceFee = subtotal * 0.05m; // 5% service fee
        
        // Get discount from latest payment if available
        var discountAmount = order.Payments?.OrderByDescending(p => p.PaymentDate ?? DateTime.MinValue).FirstOrDefault()?.DiscountAmount ?? 0;
        
        var totalAmount = order.TotalAmount ?? (subtotal + vatAmount + serviceFee - discountAmount);

        // Get payment method from latest transaction
        var latestTransaction = order.Transactions?.OrderByDescending(t => t.CreatedAt).FirstOrDefault();
        var paymentMethod = latestTransaction?.PaymentMethod ?? "N/A";
        
        // Get confirmed by user
        var confirmedBy = latestTransaction?.ConfirmedByUser?.FullName ?? "N/A";
        var paidAt = latestTransaction?.CompletedAt ?? order.CreatedAt ?? DateTime.UtcNow;

        // Get table number
        var tableNumber = order.Reservation?.ReservationTables?.FirstOrDefault()?.Table?.TableNumber?.ToString() ?? "N/A";

        // Create receipts directory if it doesn't exist
        var receiptsPath = Path.Combine(_webRootPath, "receipts");
        if (!Directory.Exists(receiptsPath))
        {
            Directory.CreateDirectory(receiptsPath);
        }

        var pdfFileName = $"{orderCode}.pdf";
        var pdfPath = Path.Combine(receiptsPath, pdfFileName);

        // Generate PDF using QuestPDF
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);

                // Header
                page.Header()
                    .Column(column =>
                    {
                        column.Item().AlignCenter().Text("HÓA ĐƠN THANH TOÁN")
                            .FontSize(20)
                            .Bold()
                            .FontColor(Colors.Blue.Darken3);

                        column.Item().PaddingTop(5);
                        column.Item().AlignCenter().Text($"#{orderCode}")
                            .FontSize(16)
                            .Bold();
                    });

                // Content
                page.Content()
                    .PaddingVertical(10)
                    .Column(column =>
                    {
                        // Order Information
                        column.Item().Text($"Ngày: {paidAt:dd/MM/yyyy HH:mm:ss}")
                            .FontSize(10);
                        column.Item().Text($"Bàn: {tableNumber}")
                            .FontSize(10);
                        column.Item().Text($"Thu ngân: {confirmedBy}")
                            .FontSize(10);
                        column.Item().Text($"Phương thức thanh toán: {paymentMethod}")
                            .FontSize(10);
                        column.Item().PaddingBottom(10);

                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);

                        // Items Table
                        column.Item().Table(table =>
                        {
                            // Define columns
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4); // Item name
                                columns.RelativeColumn(1.5f); // Quantity
                                columns.RelativeColumn(2); // Unit price
                                columns.RelativeColumn(2.5f); // Total
                            });

                            // Table header
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Tên món").Bold();
                                header.Cell().Element(CellStyle).AlignRight().Text("SL").Bold();
                                header.Cell().Element(CellStyle).AlignRight().Text("Đơn giá").Bold();
                                header.Cell().Element(CellStyle).AlignRight().Text("Thành tiền").Bold();
                            });

                            // Table rows
                            if (order.OrderDetails != null && order.OrderDetails.Any())
                            {
                                foreach (var item in order.OrderDetails)
                                {
                                    var itemName = item.MenuItem?.Name ?? "N/A";
                                    var quantity = item.Quantity;
                                    var unitPrice = item.UnitPrice;
                                    var itemTotal = unitPrice * quantity;

                                    table.Cell().Element(CellStyle).Text(itemName);
                                    table.Cell().Element(CellStyle).AlignRight().Text(quantity.ToString());
                                    table.Cell().Element(CellStyle).AlignRight().Text($"{unitPrice:N0} ₫");
                                    table.Cell().Element(CellStyle).AlignRight().Text($"{itemTotal:N0} ₫");
                                }
                            }
                        });

                        column.Item().PaddingTop(10);
                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);

                        // Summary
                        column.Item().AlignRight().Column(summaryColumn =>
                        {
                            summaryColumn.Item().Text($"Tổng tạm tính: {subtotal:N0} ₫")
                                .FontSize(10);
                            summaryColumn.Item().Text($"Thuế VAT (10%): {vatAmount:N0} ₫")
                                .FontSize(10);
                            summaryColumn.Item().Text($"Phí dịch vụ (5%): {serviceFee:N0} ₫")
                                .FontSize(10);
                            
                            if (discountAmount > 0)
                            {
                                summaryColumn.Item().Text($"Giảm giá: -{discountAmount:N0} ₫")
                                    .FontSize(10)
                                    .FontColor(Colors.Red.Darken2);
                            }

                            summaryColumn.Item().PaddingTop(5);
                            summaryColumn.Item()
                                .Text($"TỔNG CỘNG: {totalAmount:N0} ₫")
                                .FontSize(14)
                                .Bold()
                                .FontColor(Colors.Blue.Darken3);
                        });
                    });

                // Footer
                page.Footer()
                    .AlignCenter()
                    .Text("Cảm ơn quý khách! Hẹn gặp lại 💚")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Darken1);
            });
        });

        // Generate PDF file
        document.GeneratePdf(pdfPath);

        // Return relative URL path
        return $"/receipts/{pdfFileName}";
    }

    /// <summary>
    /// Cell style helper for table cells
    /// </summary>
    private static IContainer CellStyle(IContainer container)
    {
        return container
            .BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(5)
            .PaddingHorizontal(5);
    }
}

