using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Enums;
using DomainAccessLayer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<ReceiptService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ICloudinaryService? _cloudinaryService;

    public ReceiptService(
        IUnitOfWork unitOfWork, 
        string webRootPath, 
        ILogger<ReceiptService> logger, 
        IConfiguration configuration,
        IServiceProvider? serviceProvider = null)
    {
        _unitOfWork = unitOfWork;
        _webRootPath = webRootPath ?? throw new ArgumentNullException(nameof(webRootPath));
        _logger = logger;
        _configuration = configuration;
        
        // Get CloudinaryService from DI if available (optional dependency)
        try
        {
            _cloudinaryService = serviceProvider?.GetService<ICloudinaryService>();
            if (_cloudinaryService != null)
            {
                _logger.LogInformation("CloudinaryService is available. Receipts will be uploaded to Cloudinary.");
            }
            else
            {
                _logger.LogInformation("CloudinaryService is not available. Receipts will be stored locally only.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get CloudinaryService. Receipts will be stored locally only.");
            _cloudinaryService = null;
        }
    }

    /// <summary>
    /// Làm tròn lên mệnh giá 1000 VND
    /// Ví dụ: 157600 → 158000, 157400 → 158000, 157000 → 157000
    /// </summary>
    private static decimal RoundUpToThousand(decimal amount)
    {
        if (amount <= 0) return 0;
        return Math.Ceiling(amount / 1000m) * 1000m;
    }

    public async Task<string> GenerateReceiptPdfAsync(int orderId, CancellationToken ct = default)
    {
        // Get order with all related data (includes OrderDetails, MenuItem, Transactions, etc.)
        _logger.LogInformation("Starting PDF receipt generation for order {OrderId}", orderId);
        var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(orderId);
        if (order == null)
        {
            _logger.LogWarning("Cannot generate receipt for order {OrderId}: order not found", orderId);
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {orderId}");
        }

        // Check if order is paid
        if (!IsPaidStatus(order.Status))
        {
            _logger.LogWarning("Cannot generate receipt for order {OrderId}: status {Status}", orderId, order.Status);
            throw new InvalidOperationException($"Đơn hàng chưa được thanh toán. Trạng thái hiện tại: {order.Status}");
        }

        // Generate order code (Controller will build same format when returning file)
        var orderCode = $"RMS{orderId:D6}";

        // Calculate amounts with new billing logic
        decimal subtotal = 0;
        if (order.OrderDetails != null && order.OrderDetails.Any())
        {
            foreach (var od in order.OrderDetails)
            {
                // Bỏ qua món đã bị xóa hoặc đã hủy
                var status = (od.Status ?? "").Trim();
                var statusLower = status.ToLower();
                
                if (statusLower == "removed" || statusLower == "cancelled" || statusLower == "đã hủy")
                {
                    continue;
                }

                //  LOGIC MỚI: Chỉ tính tiền món có Status = "Cooking", "Done", "Ready"
                // Không tính tiền món có Status = "Pending"
                
                // Danh sách status được phép thanh toán
                var billableStatuses = new[] { "cooking", "done", "ready", "served", "đang chế biến", "đã xong", "sẵn sàng" };
                bool isBillable = billableStatuses.Any(s => statusLower == s);

                //  XỬ LÝ COMBO: Nếu là combo, kiểm tra OrderComboItems
                if (od.ComboId.HasValue && od.OrderComboItems != null && od.OrderComboItems.Any())
                {
                    // Bỏ qua các món đã bị hủy trong combo khi kiểm tra
                    var activeComboItems = od.OrderComboItems.Where(oci =>
                    {
                        var comboItemStatus = (oci.Status ?? "").Trim().ToLower();
                        return comboItemStatus != "cancelled" && comboItemStatus != "đã hủy" && comboItemStatus != "removed";
                    }).ToList();

                    // Nếu không còn món nào active trong combo → không tính tiền
                    if (!activeComboItems.Any())
                    {
                        continue;
                    }

                    // Nếu có ít nhất 1 món trong combo đã sẵn sàng (Cooking/Done/Ready) thì thanh toán toàn bộ combo
                    bool hasReadyComboItem = activeComboItems.Any(oci =>
                    {
                        var comboItemStatus = (oci.Status ?? "").Trim().ToLower();
                        return billableStatuses.Any(s => comboItemStatus == s);
                    });

                    if (!hasReadyComboItem)
                    {
                        // Combo chưa có món nào sẵn sàng → không tính tiền
                        continue;
                    }
                    // Nếu có món sẵn sàng → tính tiền toàn bộ combo (logic bên dưới)
                }
                else if (!isBillable)
                {
                    // Món lẻ chưa sẵn sàng (Status = "Pending") → không tính tiền
                    continue;
                }

                int billableQuantity;
                
                // Apply billing logic based on item type
                if (od.MenuItem?.BillingType == ItemBillingType.ConsumptionBased)
                {
                    // Consumption-based items: charge for quantity used
                    billableQuantity = od.QuantityUsed ?? od.Quantity;
                }
                else
                {
                    // Kitchen-prepared items: always charge for full quantity ordered
                    billableQuantity = od.Quantity;
                }
                
                subtotal += od.UnitPrice * billableQuantity;
            }
        }
        
        //  Làm tròn Subtotal lên mệnh giá 1000
        subtotal = RoundUpToThousand(subtotal);
        
        // Tính VAT (10%) từ Subtotal đã làm tròn
        var vatAmount = RoundUpToThousand(subtotal * 0.1m);
        
        // Tính phí dịch vụ (5%) từ Subtotal đã làm tròn
        var serviceFee = RoundUpToThousand(subtotal * 0.05m);
        
        // Get discount from latest payment if available và làm tròn
        var discountAmount = RoundUpToThousand(
            order.Payments?.OrderByDescending(p => p.PaymentDate ?? DateTime.MinValue).FirstOrDefault()?.DiscountAmount ?? 0
        );
        
        // Tính tổng cộng và làm tròn
        var totalAmount = order.TotalAmount ?? (subtotal + vatAmount + serviceFee - discountAmount);
        totalAmount = RoundUpToThousand(totalAmount);

        // Get payment method from latest transaction
        var latestTransaction = order.Transactions?.OrderByDescending(t => t.CreatedAt).FirstOrDefault();
        var paymentMethod = latestTransaction?.PaymentMethod ?? "N/A";
        
        // Get confirmed by user
        var confirmedBy = latestTransaction?.ConfirmedByUser?.FullName ?? "N/A";
        var paidAt = latestTransaction?.CompletedAt ?? order.CreatedAt ?? DateTime.Now;

        // Get table number
        var tableNumber = order.Reservation?.ReservationTables?.FirstOrDefault()?.Table?.TableNumber?.ToString() ?? "N/A";

        // Get customer name if available
        var customerName = order.Customer?.User?.FullName ?? "Khách vãng lai";

        // Get restaurant info from configuration (with defaults)
        var restaurantName = _configuration["ReceiptSettings:RestaurantName"] ?? "SAPA FO REST";
        var restaurantAddress = _configuration["ReceiptSettings:RestaurantAddress"] ?? "123 Đường ABC, Quận XYZ, TP.HCM";
        var restaurantPhone = _configuration["ReceiptSettings:RestaurantPhone"] ?? "0123 456 789";

        // Format payment method in uppercase
        var paymentMethodUpper = paymentMethod.ToUpperInvariant();
        if (paymentMethodUpper.Contains("CASH"))
            paymentMethodUpper = "TIỀN MẶT";
        else if (paymentMethodUpper.Contains("VIETQR") || paymentMethodUpper.Contains("CHUYỂN KHOẢN"))
            paymentMethodUpper = "CHUYỂN KHOẢN";
        else if (paymentMethodUpper.Contains("COMBINED") || paymentMethodUpper.Contains("KẾT HỢP"))
            paymentMethodUpper = "KẾT HỢP";

        // Convert amount to Vietnamese words
        var amountInWordsRaw = ConvertNumberToVietnameseWords(totalAmount);
        // Capitalize first letter
        var amountInWords = char.ToUpperInvariant(amountInWordsRaw[0]) + amountInWordsRaw.Substring(1);

        // Create receipts directory if it doesn't exist
        var receiptsPath = Path.Combine(_webRootPath, "receipts");
        if (!Directory.Exists(receiptsPath))
        {
            Directory.CreateDirectory(receiptsPath);
        }

        var pdfFileName = $"{orderCode}.pdf";
        var pdfPath = Path.Combine(receiptsPath, pdfFileName);

        // Generate PDF using QuestPDF
        _logger.LogInformation("Composed receipt document for order {OrderId}. Totals: subtotal {Subtotal}, VAT {Vat}, service fee {ServiceFee}, discount {Discount}, total {Total}", orderId, subtotal, vatAmount, serviceFee, discountAmount, totalAmount);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);

                // Restaurant Header
                page.Header()
                    .Column(column =>
                    {
                        // Restaurant name (bold, larger)
                        column.Item().AlignCenter().Text(restaurantName)
                            .FontSize(18)
                            .Bold()
                            .FontColor(Colors.Black);

                        column.Item().PaddingTop(3);

                        // Address and phone (smaller font)
                        column.Item().AlignCenter().Text(restaurantAddress)
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken2);

                        column.Item().AlignCenter().Text($"ĐT: {restaurantPhone}")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken2);

                        column.Item().PaddingTop(8);

                        // Title "HÓA ĐƠN THANH TOÁN"
                        column.Item().AlignCenter().Text("HÓA ĐƠN THANH TOÁN")
                            .FontSize(16)
                            .Bold()
                            .FontColor(Colors.Black);

                        column.Item().PaddingTop(5);

                        // Invoice number and date on same line
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().AlignLeft().Text($"Số HĐ: {orderId:D4}")
                                .FontSize(10)
                                .Bold();
                            row.RelativeItem().AlignRight().Text($"Ngày: {paidAt:dd/MM/yyyy HH:mm}")
                                .FontSize(10);
                        });

                        column.Item().PaddingTop(3);
                    });

                // Content
                page.Content()
                    .PaddingVertical(8)
                    .Column(column =>
                    {
                        // Order Information Section
                        column.Item().Column(infoColumn =>
                        {
                            infoColumn.Item().Text($"Bàn: {tableNumber}")
                                .FontSize(10);
                            infoColumn.Item().Text($"Thu ngân: {confirmedBy}")
                                .FontSize(10);
                            infoColumn.Item().Text($"Khách hàng: {customerName}")
                                .FontSize(10);
                        });

                        column.Item().PaddingTop(5);
                        column.Item().LineHorizontal(1).LineColor(Colors.Black);

                        // Items Table with numbering
                        column.Item().PaddingTop(5).Table(table =>
                        {
                            // Define columns: Number, Item name, Quantity, Unit price, Total
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(25); // Number column
                                columns.RelativeColumn(3.5f); // Item name
                                columns.ConstantColumn(30); // Quantity
                                columns.RelativeColumn(2); // Unit price
                                columns.RelativeColumn(2.5f); // Total
                            });

                            // Table header
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("STT").Bold().FontSize(9);
                                header.Cell().Element(CellStyle).Text("Tên món").Bold().FontSize(9);
                                header.Cell().Element(CellStyle).AlignRight().Text("SL").Bold().FontSize(9);
                                header.Cell().Element(CellStyle).AlignRight().Text("Đơn giá").Bold().FontSize(9);
                                header.Cell().Element(CellStyle).AlignRight().Text("Thành tiền").Bold().FontSize(9);
                            });

                            // Table rows with numbering
                            if (order.OrderDetails != null && order.OrderDetails.Any())
                            {
                                int itemNumber = 1;
                                foreach (var item in order.OrderDetails)
                                {
                                    var itemName = item.MenuItem?.Name ?? "N/A";
                                    var quantity = item.Quantity;
                                    var unitPrice = item.UnitPrice;
                                    var itemTotal = unitPrice * quantity;

                                    table.Cell().Element(CellStyle).AlignCenter().Text($"({itemNumber})").FontSize(9);
                                    table.Cell().Element(CellStyle).Text(itemName).FontSize(9);
                                    table.Cell().Element(CellStyle).AlignRight().Text(quantity.ToString()).FontSize(9);
                                    table.Cell().Element(CellStyle).AlignRight().Text($"{unitPrice:N0} đ").FontSize(9);
                                    table.Cell().Element(CellStyle).AlignRight().Text($"{itemTotal:N0} đ").FontSize(9).Bold();

                                    itemNumber++;
                                }
                            }
                        });

                        column.Item().PaddingTop(8);
                        column.Item().LineHorizontal(1).LineColor(Colors.Black);

                        // Totals Section
                        column.Item().PaddingTop(5).AlignRight().Column(summaryColumn =>
                        {
                            summaryColumn.Item().Row(row =>
                            {
                                row.RelativeItem().AlignLeft().Text("Tổng cộng:").FontSize(10);
                                row.RelativeItem().AlignRight().Text($"{subtotal:N0} đ").FontSize(10).Bold();
                            });

                            summaryColumn.Item().Row(row =>
                            {
                                row.RelativeItem().AlignLeft().Text("VAT (10%):").FontSize(10);
                                row.RelativeItem().AlignRight().Text($"{vatAmount:N0} đ").FontSize(10);
                            });

                            summaryColumn.Item().Row(row =>
                            {
                                row.RelativeItem().AlignLeft().Text("Phí dịch vụ (5%):").FontSize(10);
                                row.RelativeItem().AlignRight().Text($"{serviceFee:N0} đ").FontSize(10);
                            });

                            if (discountAmount > 0)
                            {
                                summaryColumn.Item().Row(row =>
                                {
                                    row.RelativeItem().AlignLeft().Text("Giảm giá:").FontSize(10).FontColor(Colors.Red.Darken2);
                                    row.RelativeItem().AlignRight().Text($"-{discountAmount:N0} đ").FontSize(10).FontColor(Colors.Red.Darken2).Bold();
                                });
                            }

                            summaryColumn.Item().PaddingTop(3);
                            summaryColumn.Item().LineHorizontal(1).LineColor(Colors.Black);

                            summaryColumn.Item().PaddingTop(3);
                            summaryColumn.Item().Row(row =>
                            {
                                row.RelativeItem().AlignLeft().Text("TỔNG CỘNG:").FontSize(12).Bold();
                                row.RelativeItem().AlignRight().Text($"{totalAmount:N0} đ").FontSize(12).Bold();
                            });

                            summaryColumn.Item().PaddingTop(5);
                            summaryColumn.Item().Row(row =>
                            {
                                row.RelativeItem().AlignLeft().Text("Phương thức:").FontSize(10);
                                row.RelativeItem().AlignRight().Text(paymentMethodUpper).FontSize(10).Bold();
                            });

                            summaryColumn.Item().PaddingTop(3);
                            summaryColumn.Item().Text($"Bằng chữ: {amountInWords}")
                                .FontSize(9)
                                .Italic()
                                .FontColor(Colors.Grey.Darken1);
                        });
                    });

                // Footer
                page.Footer()
                    .PaddingTop(10)
                    .AlignCenter()
                    .Text("Cảm ơn Quý Khách – Hẹn Gặp Lại!")
                    .FontSize(11)
                    .Bold()
                    .FontColor(Colors.Black);
            });
        });

        // Generate PDF file
        try
        {
            document.GeneratePdf(pdfPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF for order {OrderId}", orderId);
            throw;
        }

        long fileSize = 0;
        if (System.IO.File.Exists(pdfPath))
        {
            fileSize = new FileInfo(pdfPath).Length;
        }

        _logger.LogInformation("Finished generating receipt for order {OrderId}. File saved to {PdfPath} ({FileSize} bytes)", orderId, pdfPath, fileSize);

        //  Upload PDF to Cloudinary if service is available
        string? cloudinaryUrl = null;
        if (_cloudinaryService != null)
        {
            try
            {
                var pdfBytes = await System.IO.File.ReadAllBytesAsync(pdfPath, ct);
                cloudinaryUrl = await _cloudinaryService.UploadPdfAsync(pdfBytes, pdfFileName, "receipts");
                
                if (!string.IsNullOrEmpty(cloudinaryUrl))
                {
                    _logger.LogInformation("Successfully uploaded receipt PDF to Cloudinary for order {OrderId}. URL: {CloudinaryUrl}", orderId, cloudinaryUrl);
                }
                else
                {
                    _logger.LogWarning("Failed to upload receipt PDF to Cloudinary for order {OrderId}. Will use local file.", orderId);
                }
            }
            catch (Exception cloudinaryEx)
            {
                // Log error but don't fail - fallback to local storage
                _logger.LogWarning(cloudinaryEx, "Error uploading receipt PDF to Cloudinary for order {OrderId}. Will use local file.", orderId);
            }
        }

        // Return Cloudinary URL if available, otherwise return local path
        return cloudinaryUrl ?? $"/receipts/{pdfFileName}";
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

    private static bool IsPaidStatus(string? status)
        => string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Converts a number to Vietnamese words (e.g., 790000 -> "Bảy trăm chín mươi nghìn đồng chẵn")
    /// </summary>
    private static string ConvertNumberToVietnameseWords(decimal amount)
    {
        if (amount == 0)
            return "Không đồng";

        var wholePart = (long)Math.Floor(amount);
        var fractionalPart = (long)((amount - wholePart) * 100);

        var words = ConvertNumberToWords(wholePart);
        var result = words + " đồng";

        if (fractionalPart > 0)
        {
            result += " " + ConvertNumberToWords(fractionalPart) + " xu";
        }
        else
        {
            result += " chẵn";
        }

        return result;
    }

    private static string ConvertNumberToWords(long number)
    {
        if (number == 0)
            return "không";

        if (number < 0)
            return "âm " + ConvertNumberToWords(-number);

        string[] ones = { "", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
        string[] tens = { "", "mười", "hai mươi", "ba mươi", "bốn mươi", "năm mươi", "sáu mươi", "bảy mươi", "tám mươi", "chín mươi" };
        string[] hundreds = { "", "một trăm", "hai trăm", "ba trăm", "bốn trăm", "năm trăm", "sáu trăm", "bảy trăm", "tám trăm", "chín trăm" };

        if (number < 10)
            return ones[number];

        if (number < 100)
        {
            var ten = number / 10;
            var one = number % 10;
            if (one == 0)
                return tens[ten];
            if (one == 1 && ten == 1)
                return "mười một";
            if (one == 1 && ten > 1)
                return tens[ten] + " mốt";
            if (one == 5 && ten > 1)
                return tens[ten] + " lăm";
            return tens[ten] + " " + ones[one];
        }

        if (number < 1000)
        {
            var hundred = number / 100;
            var remainder = number % 100;
            if (remainder == 0)
                return hundreds[hundred];
            return hundreds[hundred] + " " + ConvertNumberToWords(remainder);
        }

        if (number < 1_000_000)
        {
            var thousand = number / 1000;
            var remainder = number % 1000;
            var thousandWords = ConvertNumberToWords(thousand) + " nghìn";
            if (remainder == 0)
                return thousandWords;
            if (remainder < 100)
                return thousandWords + " không trăm " + ConvertNumberToWords(remainder);
            return thousandWords + " " + ConvertNumberToWords(remainder);
        }

        if (number < 1_000_000_000)
        {
            var million = number / 1_000_000;
            var remainder = number % 1_000_000;
            var millionWords = ConvertNumberToWords(million) + " triệu";
            if (remainder == 0)
                return millionWords;
            if (remainder < 1000)
                return millionWords + " không nghìn " + ConvertNumberToWords(remainder);
            return millionWords + " " + ConvertNumberToWords(remainder);
        }

        var billion = number / 1_000_000_000;
        var billionRemainder = number % 1_000_000_000;
        var billionWords = ConvertNumberToWords(billion) + " tỷ";
        if (billionRemainder == 0)
            return billionWords;
        return billionWords + " " + ConvertNumberToWords(billionRemainder);
    }

    /// <summary>
    /// Generate PDF receipt for a paid reservation (tổng hợp tất cả Orders)
    /// </summary>
    public async Task<string> GenerateReceiptPdfByReservationAsync(int reservationId, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting PDF receipt generation for reservation {ReservationId}", reservationId);

        // Lấy tất cả Orders của Reservation
        var orders = await _unitOfWork.Payments.GetOrdersByReservationIdAsync(reservationId);
        var ordersList = orders.ToList();

        if (!ordersList.Any())
        {
            _logger.LogWarning("Cannot generate receipt for reservation {ReservationId}: no orders found", reservationId);
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng nào cho Reservation với ID: {reservationId}");
        }

        // Kiểm tra tất cả Orders đã được thanh toán
        var unpaidOrders = ordersList.Where(o => !IsPaidStatus(o.Status)).ToList();
        if (unpaidOrders.Any())
        {
            _logger.LogWarning("Cannot generate receipt for reservation {ReservationId}: {Count} orders not paid", reservationId, unpaidOrders.Count);
            throw new InvalidOperationException($"Có {unpaidOrders.Count} đơn hàng chưa được thanh toán. Vui lòng thanh toán tất cả đơn hàng trước khi tạo hóa đơn.");
        }

        // Lấy thông tin Reservation từ order đầu tiên
        var firstOrder = ordersList.First();
        var reservation = firstOrder.Reservation;
        if (reservation == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Reservation với ID: {reservationId}");
        }

        // Lấy thông tin Customer
        var customer = reservation.Customer ?? firstOrder.Customer;
        var customerName = customer?.User?.FullName ?? reservation.CustomerNameReservation ?? "Khách vãng lai";
        var customerPhone = customer?.User?.Phone ?? "";

        // Lấy thông tin bàn
        var tableNumbers = new List<string>();
        if (reservation.ReservationTables != null && reservation.ReservationTables.Any())
        {
            tableNumbers = reservation.ReservationTables
                .Where(rt => rt.Table != null && !string.IsNullOrEmpty(rt.Table.TableNumber))
                .Select(rt => rt.Table!.TableNumber!)
                .ToList();
        }
        var tableNumber = tableNumbers.Any() ? string.Join(", ", tableNumbers) : "—";

        // Lấy thông tin Staff (thu ngân từ Transaction)
        var confirmedBy = "Thu ngân";
        var transactions = ordersList
            .SelectMany(o => o.Transactions ?? new List<Transaction>())
            .Where(t => t.IsManualConfirmed && t.ConfirmedByUser != null)
            .OrderByDescending(t => t.CompletedAt)
            .ToList();
        
        if (transactions.Any())
        {
            confirmedBy = transactions.First().ConfirmedByUser?.FullName ?? "Thu ngân";
        }

        // Lấy PaymentMethod từ Transaction
        var paymentMethod = transactions.Any() 
            ? transactions.First().PaymentMethod ?? "Tiền mặt" 
            : "Tiền mặt";

        // Tính tổng tất cả OrderDetails từ tất cả Orders
        decimal subtotal = 0;
        var allOrderDetails = new List<OrderDetail>();

        foreach (var order in ordersList)
        {
            if (order.OrderDetails != null && order.OrderDetails.Any())
            {
                foreach (var od in order.OrderDetails)
                {
                    // Bỏ qua món đã bị xóa hoặc đã hủy
                    var status = (od.Status ?? "").Trim();
                    var statusLower = status.ToLower();
                    
                    if (statusLower == "removed" || statusLower == "cancelled" || statusLower == "đã hủy")
                    {
                        continue;
                    }

                    // Chỉ tính tiền món có Status = "Cooking", "Done", "Ready"
                    var billableStatuses = new[] { "cooking", "done", "ready", "served", "đang chế biến", "đã xong", "sẵn sàng" };
                    bool isBillable = billableStatuses.Any(s => statusLower == s);

                    // XỬ LÝ COMBO
                    if (od.ComboId.HasValue && od.OrderComboItems != null && od.OrderComboItems.Any())
                    {
                        var activeComboItems = od.OrderComboItems.Where(oci =>
                        {
                            var comboItemStatus = (oci.Status ?? "").Trim().ToLower();
                            return comboItemStatus != "cancelled" && comboItemStatus != "đã hủy" && comboItemStatus != "removed";
                        }).ToList();

                        if (!activeComboItems.Any())
                        {
                            continue;
                        }

                        bool hasReadyComboItem = activeComboItems.Any(oci =>
                        {
                            var comboItemStatus = (oci.Status ?? "").Trim().ToLower();
                            return billableStatuses.Any(s => comboItemStatus == s);
                        });

                        if (!hasReadyComboItem)
                        {
                            continue;
                        }
                    }
                    else if (!isBillable)
                    {
                        continue;
                    }

                    // Tính tiền
                    int billableQuantity;
                    if (od.MenuItem?.BillingType == ItemBillingType.ConsumptionBased)
                    {
                        billableQuantity = (od.QuantityUsed.HasValue && od.QuantityUsed > 0)
    ? od.QuantityUsed.Value
    : od.Quantity;
                    }
                    else
                    {
                        billableQuantity = od.Quantity;
                    }

                    subtotal += od.UnitPrice * billableQuantity;
                    allOrderDetails.Add(od);
                }
            }
        }

        // Tính VAT, Service Fee, Discount, Total (sử dụng logic từ PaymentService)
        var vatAmount = subtotal * 0.1m;
        var serviceFee = subtotal * 0.05m;
        
        // Lấy discount từ Transaction hoặc Order (nếu có)
        decimal discountAmount = 0;
        // TODO: Có thể lấy discount từ voucher/promotion nếu cần

        var totalAmount = subtotal + vatAmount + serviceFee - discountAmount;

        // Lấy PaidAt từ Transaction
        var paidAt = transactions.Any() && transactions.First().CompletedAt.HasValue
            ? transactions.First().CompletedAt.Value
            : DateTime.Now;

        // Generate reservation code
        var reservationCode = $"RES{reservationId:D6}";

        // Restaurant info từ config
        var restaurantName = _configuration["ReceiptSettings:RestaurantName"] ?? "Nhà hàng Sapa Forest";
        var restaurantAddress = _configuration["ReceiptSettings:RestaurantAddress"] ?? "Địa chỉ nhà hàng";
        var restaurantPhone = _configuration["ReceiptSettings:RestaurantPhone"] ?? "0123 456 789";

        // Format payment method
        var paymentMethodUpper = paymentMethod.ToUpperInvariant();
        if (paymentMethodUpper.Contains("CASH"))
            paymentMethodUpper = "TIỀN MẶT";
        else if (paymentMethodUpper.Contains("VIETQR") || paymentMethodUpper.Contains("CHUYỂN KHOẢN"))
            paymentMethodUpper = "CHUYỂN KHOẢN";
        else if (paymentMethodUpper.Contains("COMBINED") || paymentMethodUpper.Contains("KẾT HỢP"))
            paymentMethodUpper = "KẾT HỢP";

        // Convert amount to Vietnamese words
        var amountInWordsRaw = ConvertNumberToVietnameseWords(totalAmount);
        var amountInWords = char.ToUpperInvariant(amountInWordsRaw[0]) + amountInWordsRaw.Substring(1);

        // Create receipts directory
        var receiptsPath = Path.Combine(_webRootPath, "receipts");
        if (!Directory.Exists(receiptsPath))
        {
            Directory.CreateDirectory(receiptsPath);
        }

        var pdfFileName = $"{reservationCode}.pdf";
        var pdfPath = Path.Combine(receiptsPath, pdfFileName);

        // Generate PDF
        _logger.LogInformation("Composed receipt document for reservation {ReservationId}. Totals: subtotal {Subtotal}, VAT {Vat}, service fee {ServiceFee}, discount {Discount}, total {Total}", 
            reservationId, subtotal, vatAmount, serviceFee, discountAmount, totalAmount);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);

                // Restaurant Header
                page.Header()
                    .Column(column =>
                    {
                        column.Item().AlignCenter().Text(restaurantName)
                            .FontSize(18)
                            .Bold()
                            .FontColor(Colors.Black);

                        column.Item().PaddingTop(3);

                        column.Item().AlignCenter().Text(restaurantAddress)
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken2);

                        column.Item().AlignCenter().Text($"ĐT: {restaurantPhone}")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken2);

                        column.Item().PaddingTop(8);

                        column.Item().AlignCenter().Text("HÓA ĐƠN THANH TOÁN")
                            .FontSize(16)
                            .Bold()
                            .FontColor(Colors.Black);

                        column.Item().PaddingTop(5);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().AlignLeft().Text($"Số HĐ: {reservationCode}")
                                .FontSize(10)
                                .Bold();
                            row.RelativeItem().AlignRight().Text($"Ngày: {paidAt:dd/MM/yyyy HH:mm}")
                                .FontSize(10);
                        });

                        column.Item().PaddingTop(3);
                    });

                // Content
                page.Content()
                    .PaddingVertical(8)
                    .Column(column =>
                    {
                        // Order Information Section
                        column.Item().Column(infoColumn =>
                        {
                            infoColumn.Item().Text($"Bàn: {tableNumber}")
                                .FontSize(10);
                            infoColumn.Item().Text($"Thu ngân: {confirmedBy}")
                                .FontSize(10);
                            infoColumn.Item().Text($"Khách hàng: {customerName}")
                                .FontSize(10);
                            if (!string.IsNullOrEmpty(customerPhone))
                            {
                                infoColumn.Item().Text($"ĐT: {customerPhone}")
                                    .FontSize(10);
                            }
                            infoColumn.Item().Text($"Số đơn: {ordersList.Count} đơn")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);
                        });

                        column.Item().PaddingTop(5);
                        column.Item().LineHorizontal(1).LineColor(Colors.Black);

                        // Items Table
                        column.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(25); // Number
                                columns.RelativeColumn(3.5f); // Item name
                                columns.ConstantColumn(30); // Quantity
                                columns.RelativeColumn(2); // Unit price
                                columns.RelativeColumn(2.5f); // Total
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("STT").Bold().FontSize(9);
                                header.Cell().Element(CellStyle).Text("Tên món").Bold().FontSize(9);
                                header.Cell().Element(CellStyle).AlignRight().Text("SL").Bold().FontSize(9);
                                header.Cell().Element(CellStyle).AlignRight().Text("Đơn giá").Bold().FontSize(9);
                                header.Cell().Element(CellStyle).AlignRight().Text("Thành tiền").Bold().FontSize(9);
                            });

                            if (allOrderDetails.Any())
                            {
                                int itemNumber = 1;
                                foreach (var item in allOrderDetails)
                                {
                                    var itemName = item.MenuItem?.Name ?? item.Combo?.Name ?? "N/A";
                                    var quantity = item.MenuItem?.BillingType == ItemBillingType.ConsumptionBased
                                        ? (item.QuantityUsed > 0 ? item.QuantityUsed : item.Quantity)
                                        : item.Quantity;
                                    var unitPrice = item.UnitPrice;
                                    var itemTotal = unitPrice * quantity;

                                    table.Cell().Element(CellStyle).AlignCenter().Text($"({itemNumber})").FontSize(9);
                                    table.Cell().Element(CellStyle).Text(itemName).FontSize(9);
                                    table.Cell().Element(CellStyle).AlignRight().Text(quantity.ToString()).FontSize(9);
                                    table.Cell().Element(CellStyle).AlignRight().Text($"{unitPrice:N0} đ").FontSize(9);
                                    table.Cell().Element(CellStyle).AlignRight().Text($"{itemTotal:N0} đ").FontSize(9).Bold();

                                    itemNumber++;
                                }
                            }
                        });

                        column.Item().PaddingTop(8);
                        column.Item().LineHorizontal(1).LineColor(Colors.Black);

                        // Totals Section
                        column.Item().PaddingTop(5).AlignRight().Column(summaryColumn =>
                        {
                            summaryColumn.Item().Row(row =>
                            {
                                row.RelativeItem().AlignLeft().Text("Tổng cộng:").FontSize(10);
                                row.RelativeItem().AlignRight().Text($"{subtotal:N0} đ").FontSize(10).Bold();
                            });

                            summaryColumn.Item().Row(row =>
                            {
                                row.RelativeItem().AlignLeft().Text("VAT (10%):").FontSize(10);
                                row.RelativeItem().AlignRight().Text($"{vatAmount:N0} đ").FontSize(10);
                            });

                            summaryColumn.Item().Row(row =>
                            {
                                row.RelativeItem().AlignLeft().Text("Phí dịch vụ (5%):").FontSize(10);
                                row.RelativeItem().AlignRight().Text($"{serviceFee:N0} đ").FontSize(10);
                            });

                            if (discountAmount > 0)
                            {
                                summaryColumn.Item().Row(row =>
                                {
                                    row.RelativeItem().AlignLeft().Text("Giảm giá:").FontSize(10).FontColor(Colors.Red.Darken2);
                                    row.RelativeItem().AlignRight().Text($"-{discountAmount:N0} đ").FontSize(10).FontColor(Colors.Red.Darken2).Bold();
                                });
                            }

                            summaryColumn.Item().PaddingTop(3);
                            summaryColumn.Item().LineHorizontal(1).LineColor(Colors.Black);

                            summaryColumn.Item().PaddingTop(3);
                            summaryColumn.Item().Row(row =>
                            {
                                row.RelativeItem().AlignLeft().Text("TỔNG CỘNG:").FontSize(12).Bold();
                                row.RelativeItem().AlignRight().Text($"{totalAmount:N0} đ").FontSize(12).Bold();
                            });

                            summaryColumn.Item().PaddingTop(5);
                            summaryColumn.Item().Row(row =>
                            {
                                row.RelativeItem().AlignLeft().Text("Phương thức:").FontSize(10);
                                row.RelativeItem().AlignRight().Text(paymentMethodUpper).FontSize(10).Bold();
                            });

                            summaryColumn.Item().PaddingTop(3);
                            summaryColumn.Item().Text($"Bằng chữ: {amountInWords}")
                                .FontSize(9)
                                .Italic()
                                .FontColor(Colors.Grey.Darken1);
                        });

                        column.Item().PaddingTop(10);
                        column.Item().AlignCenter().Text("Cảm ơn quý khách!")
                            .FontSize(10)
                            .Italic()
                            .FontColor(Colors.Grey.Darken1);
                    });
            });
        });

        document.GeneratePdf(pdfPath);

        _logger.LogInformation("PDF receipt generated successfully for reservation {ReservationId} at {PdfPath}", reservationId, pdfPath);

        // Backend/BusinessAccessLayer/Services/ReceiptService.cs (line 950-966)
        // Upload to Cloudinary if available
        if (_cloudinaryService != null)
        {
            try
            {
                // Read PDF file from path
                var pdfBytes = await File.ReadAllBytesAsync(pdfPath, ct);
                var cloudinaryUrl = await _cloudinaryService.UploadPdfAsync(pdfBytes, pdfFileName, "receipts");
                if (!string.IsNullOrEmpty(cloudinaryUrl))
                {
                    _logger.LogInformation("Receipt uploaded to Cloudinary for reservation {ReservationId}: {CloudinaryUrl}", reservationId, cloudinaryUrl);
                    return cloudinaryUrl;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to upload receipt to Cloudinary for reservation {ReservationId}. Using local path.", reservationId);
            }
        }

        return $"/receipts/{pdfFileName}";
    }
}

