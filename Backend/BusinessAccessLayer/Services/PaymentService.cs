using AutoMapper;
using BusinessAccessLayer.Constants;
using BusinessAccessLayer.DTOs.Payment;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Enums;
using DomainAccessLayer.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace BusinessAccessLayer.Services;

/// <summary>
/// Service xử lý business logic cho Payment
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAuditLogService _auditLogService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IKitchenDisplayService _kitchenDisplayService;

    public PaymentService(IUnitOfWork unitOfWork, IMapper mapper, IAuditLogService auditLogService, IServiceProvider serviceProvider, IKitchenDisplayService kitchenDisplayService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _auditLogService = auditLogService;
        _serviceProvider = serviceProvider;
        _kitchenDisplayService = kitchenDisplayService;
    }

    private static readonly HashSet<string> PendingStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending",
        "pending-payment",
        "WaitingForPayment",
        "Processing",
        "Confirmed"  // Đơn đã được khách xác nhận, chờ thanh toán
    };

    private static readonly HashSet<string> ProcessedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Paid",
        "Completed",
        "Success"
    };

    public async Task<OrderListResponseDto> GetOrdersAsync(DateOnly? date = default, string? statusFilter = null, string sortOrder = "desc", CancellationToken ct = default)
    {
        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        IEnumerable<Order> orders;
        if (date.HasValue)
        {
            orders = await _unitOfWork.Payments.GetOrdersByDateAsync(selectedDate);
        }
        else
        {
            orders = await _unitOfWork.Payments.GetAllOrdersWithDetailsAsync();
        }
        var orderDtos = new List<OrderDto>();

        foreach (var order in orders)
        {
            var orderDto = _mapper.Map<OrderDto>(order);
            CalculateOrderAmounts(order, orderDto);
            PopulateOrderMetadata(order, orderDto);
            orderDtos.Add(orderDto);
        }

        var pendingCount = orderDtos.Count(o => IsPendingStatus(o.Status));
        var processedCount = orderDtos.Count(o => IsProcessedStatus(o.Status));

        IEnumerable<OrderDto> filteredOrders = orderDtos;
        if (!string.IsNullOrWhiteSpace(statusFilter) && !statusFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            if (statusFilter.Equals("pending", StringComparison.OrdinalIgnoreCase))
            {
                filteredOrders = orderDtos.Where(o => IsPendingStatus(o.Status));
            }
            else if (statusFilter.Equals("processed", StringComparison.OrdinalIgnoreCase))
            {
                filteredOrders = orderDtos.Where(o => IsProcessedStatus(o.Status));
            }
        }

        filteredOrders = sortOrder?.Equals("asc", StringComparison.OrdinalIgnoreCase) == true
            ? filteredOrders.OrderBy(o => o.CreatedAt)
            : filteredOrders.OrderByDescending(o => o.CreatedAt);

        return new OrderListResponseDto
        {
            SelectedDate = selectedDate,
            TotalOrders = orderDtos.Count,
            PendingOrders = pendingCount,
            ProcessedOrders = processedCount,
            Orders = filteredOrders.ToList()
        };
    }

    public async Task<OrderDto?> GetOrderDetailAsync(int orderId, CancellationToken ct = default)
    {
        var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(orderId);
        
        if (order == null)
        {
            return null;
        }

        var orderDto = _mapper.Map<OrderDto>(order);
        
        // Tính toán các khoản tiền
        CalculateOrderAmounts(order, orderDto);
        PopulateOrderMetadata(order, orderDto);
        
        // Cập nhật lại Status của combo dựa trên trạng thái các món con trong combo (từ KDS)
        await UpdateComboStatusesFromKitchenAsync(orderDto, ct);

        return orderDto;
    }

    /// <summary>
    /// Cập nhật status của các dòng combo trong màn thanh toán
    /// dựa trên trạng thái thực tế của các món con trong combo ở KDS.
    /// </summary>
    private async Task UpdateComboStatusesFromKitchenAsync(OrderDto orderDto, CancellationToken ct)
    {
        if (orderDto == null || orderDto.OrderItems == null || orderDto.OrderItems.Count == 0)
        {
            return;
        }

        // Lấy toàn bộ items của order từ KDS (bao gồm món lẻ + món trong combo)
        var kitchenCard = await _kitchenDisplayService.GetOrderDetailsWithAllItemsAsync(orderDto.OrderId);
        if (kitchenCard == null || kitchenCard.Items == null || kitchenCard.Items.Count == 0)
        {
            return;
        }

        foreach (var item in orderDto.OrderItems.Where(i => i.ComboId.HasValue))
        {
            // Các KitchenOrderItemDto tương ứng với combo này: cùng OrderDetailId
            var relatedKitchenItems = kitchenCard.Items
                .Where(k => k.OrderDetailId == item.OrderDetailId)
                .ToList();

            if (!relatedKitchenItems.Any())
            {
                continue;
            }

            var statuses = relatedKitchenItems
                .Select(k => (k.Status ?? "Pending").Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            if (!statuses.Any())
            {
                continue;
            }

            // Quy tắc tổng hợp:
            // - Tất cả Done  -> Done
            // - Tất cả Cooking -> Cooking
            // - Tất cả Ready -> Ready
            // - Tất cả Pending -> Pending
            // - Trường hợp mix: giữ nguyên Status gốc (không override để tránh hiểu nhầm)
            var distinct = statuses.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (distinct.Count == 1)
            {
                item.Status = distinct[0];
            }
        }
    }

    public async Task<OrderDto> ApplyDiscountAsync(DiscountRequestDto request, CancellationToken ct = default)
    {
        var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(request.OrderId);
        
        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {request.OrderId}");
        }

        // Tính toán giảm giá (có thể tích hợp với VoucherService sau)
        decimal discountAmount = request.DiscountAmount ?? 0;

        // Cập nhật discount vào order (có thể lưu vào Payment record)
        var orderDto = _mapper.Map<OrderDto>(order);
        orderDto.DiscountAmount = discountAmount;
        
        // Tính lại tổng tiền
        CalculateOrderAmounts(order, orderDto);
        orderDto.TotalAmount = (orderDto.Subtotal ?? 0) + (orderDto.VatAmount ?? 0) + 
                              (orderDto.ServiceFee ?? 0) - discountAmount;

        return orderDto;
    }

    public async Task<TransactionDto> InitiatePaymentAsync(PaymentInitiateRequestDto request, CancellationToken ct = default)
    {
        var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(request.OrderId);
        
        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {request.OrderId}");
        }

        // Validate: Đơn hàng phải được khách xác nhận trước khi thanh toán
        if (string.IsNullOrEmpty(order.Status) || 
            !order.Status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Đơn hàng chưa được khách xác nhận, không thể thanh toán. Vui lòng yêu cầu khách xác nhận số lượng món đã dùng trước.");
        }

        // Tạo sessionId cho giao dịch
        var sessionId = $"SESSION-{DateTime.UtcNow.Ticks}-{request.OrderId}";

        // Tạo transaction record
        var transaction = new Transaction
        {
            OrderId = request.OrderId,
            TransactionCode = $"TXN-{DateTime.UtcNow.Ticks}",
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            SessionId = sessionId
        };

        var savedTransaction = await _unitOfWork.Payments.SaveTransactionAsync(transaction);
        
        // 🔓 GIẢI PHÓNG BÀN NGAY KHI BẮT ĐẦU THANH TOÁN
        try
        {
            var tables = await _unitOfWork.Tables.GetTablesByOrderIdAsync(request.OrderId);
            if (tables != null && tables.Any())
            {
                foreach (var table in tables)
                {
                    table.Status = "Available";
                    await _unitOfWork.Tables.UpdateAsync(table);
                    
                    // Log table release
                    await _auditLogService.LogEventAsync(
                        eventType: "table_released",
                        entityType: "Table",
                        entityId: table.TableId,
                        description: $"Bàn {table.TableNumber} được giải phóng khi bắt đầu thanh toán cho Order {request.OrderId}",
                        userId: null,
                        ct: ct
                    );
                }
                
                await _unitOfWork.Tables.SaveAsync();
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the payment - table release is secondary
            await _auditLogService.LogEventAsync(
                eventType: "table_release_failed",
                entityType: "Order",
                entityId: request.OrderId,
                description: $"Lỗi khi giải phóng bàn cho Order {request.OrderId}: {ex.Message}",
                userId: null,
                ct: ct
            );
        }
        
        return _mapper.Map<TransactionDto>(savedTransaction);
    }

    public async Task<TransactionDto> ProcessPaymentAsync(PaymentRequestDto request, CancellationToken ct = default)
    {
        var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(request.OrderId);
        
        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {request.OrderId}");
        }

        // Validate: Đơn hàng phải được khách xác nhận trước khi thanh toán
        if (string.IsNullOrEmpty(order.Status) || 
            !order.Status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Đơn hàng chưa được khách xác nhận, không thể thanh toán. Vui lòng yêu cầu khách xác nhận số lượng món đã dùng trước.");
        }

        // Validate payment amount
        var orderDto = _mapper.Map<OrderDto>(order);
        CalculateOrderAmounts(order, orderDto);
        var expectedAmount = (orderDto.Subtotal ?? 0) + (orderDto.VatAmount ?? 0) + 
                            (orderDto.ServiceFee ?? 0) - (orderDto.DiscountAmount ?? 0);

        if (request.Amount != expectedAmount)
        {
            throw new InvalidOperationException($"Số tiền thanh toán không khớp. Mong đợi: {expectedAmount}, Nhận được: {request.Amount}");
        }

        // Validate cash payment
        if (request.PaymentMethod == "Cash" && request.CashGiven.HasValue)
        {
            if (request.CashGiven.Value < request.Amount)
            {
                throw new InvalidOperationException("Số tiền khách đưa không đủ!");
            }
        }

        // Tạo transaction
        var transaction = new Transaction
        {
            OrderId = request.OrderId,
            TransactionCode = $"TXN-{DateTime.UtcNow.Ticks}",
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            Status = "Success",
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            SessionId = request.SessionId,
            Notes = request.Notes
        };

        // Cập nhật trạng thái đơn hàng
        order.Status = OrderStatusConstants.Paid;
        await _unitOfWork.Payments.UpdateAsync(order);

        // Lưu transaction
        var savedTransaction = await _unitOfWork.Payments.SaveTransactionAsync(transaction);
        
        // Save changes
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<TransactionDto>(savedTransaction);
    }

    public async Task<OrderDto> ConfirmOrderAsync(CustomerConfirmRequestDto request, CancellationToken ct = default)
    {
        var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(request.OrderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {request.OrderId}");
        }

        if (order.OrderDetails == null || !order.OrderDetails.Any())
        {
            throw new InvalidOperationException("Đơn hàng không có món để xác nhận.");
        }

        foreach (var confirmed in request.Items)
        {
            var detail = order.OrderDetails.FirstOrDefault(d => d.OrderDetailId == confirmed.OrderDetailId);
            if (detail == null)
            {
                continue;
            }

            if (confirmed.IsRemoved)
            {
                // Món bị hủy: set cả Quantity và QuantityUsed về 0
                detail.Quantity = 0;
                detail.QuantityUsed = 0;
                detail.Status = "Removed";
            }
            else
            {
                // ✅ FIX BUG: KHÔNG ghi đè Quantity (SL đặt)
                // Chỉ cập nhật QuantityUsed (SL thực tế khách dùng)
                // Giữ nguyên detail.Quantity (đây là SL ban đầu đặt)
                detail.QuantityUsed = confirmed.QuantityUsed < 0 ? 0 : confirmed.QuantityUsed;
                detail.Status = "Confirmed";
            }
        }

        // Sau khi khách xác nhận, chuyển trạng thái đơn sang "Confirmed" (đã xác nhận, chờ thanh toán)
        order.Status = OrderStatusConstants.Confirmed;

        await _unitOfWork.SaveChangesAsync();

        var orderDto = _mapper.Map<OrderDto>(order);
        CalculateOrderAmounts(order, orderDto);
        PopulateOrderMetadata(order, orderDto);
        return orderDto;
    }

    public async Task<bool> UndoConfirmOrderAsync(int orderId, UndoConfirmRequestDto request, CancellationToken ct = default)
    {
        var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(orderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {orderId}");
        }

        if (!string.Equals(order.Status, OrderStatusConstants.Confirmed, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Order cannot be reverted at this stage.");
        }

        if (order.Payments != null && order.Payments.Any(p => p.PaymentDate.HasValue))
        {
            throw new InvalidOperationException("Không thể hoàn tác vì đơn hàng đã bắt đầu thanh toán.");
        }

        if (order.OrderDetails != null && order.OrderDetails.Any(od =>
            string.Equals(od.Status, "Cooking", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(od.Status, "Served", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Không thể hoàn tác vì bếp đã bắt đầu chế biến món.");
        }

        order.Status = OrderStatusConstants.WaitingConfirmation;
        order.ConfirmedAt = null;
        order.ConfirmedByStaffId = null;

        await _unitOfWork.Payments.UpdateAsync(order);

        var staffId = await ResolveStaffIdAsync(request.StaffId, ct);

        var history = new OrderHistory
        {
            OrderId = orderId,
            Action = "Undo Confirmation",
            Reason = request.Reason,
            StaffId = staffId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Payments.AddOrderHistoryAsync(history);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private async Task<int> ResolveStaffIdAsync(int userId, CancellationToken ct = default)
    {
        var user = await _unitOfWork.StaffProfiles.GetWithDetailsAsync(userId, ct);
        var staff = user?.Staff?.FirstOrDefault();

        if (staff == null)
        {
            throw new InvalidOperationException("Không tìm thấy hồ sơ nhân viên tương ứng.");
        }

        return staff.StaffId;
    }

    public async Task<TransactionDto?> GetPaymentResultAsync(string sessionId, CancellationToken ct = default)
    {
        var transaction = await _unitOfWork.Payments.GetTransactionBySessionIdAsync(sessionId);
        
        if (transaction == null)
        {
            return null;
        }

        return _mapper.Map<TransactionDto>(transaction);
    }

    /// <summary>
    /// Tính toán các khoản tiền cho đơn hàng
    /// </summary>
    private void CalculateOrderAmounts(Order order, OrderDto orderDto)
    {
        // Tính subtotal từ OrderDetails với logic mới
        decimal subtotal = 0;
        if (order.OrderDetails != null && order.OrderDetails.Any())
        {
            foreach (var od in order.OrderDetails)
            {
                // Bỏ qua món đã bị xóa
                if (od.Status == "Removed")
                {
                    continue;
                }

                int billableQuantity;
                
                // ✅ LOGIC MỚI: Phân biệt 2 loại món
                if (od.MenuItem?.BillingType == ItemBillingType.ConsumptionBased)
                {
                    // (A) Món tiêu hao: Tính tiền theo SL thực tế khách dùng
                    // Nếu chưa confirm (QuantityUsed = null), fallback về Quantity
                    billableQuantity = od.QuantityUsed ?? od.Quantity;
                }
                else
                {
                    // (B) Món bếp chế biến: LUÔN tính theo SL đặt (100%)
                    // Bếp đã nấu thì phải thanh toán đủ
                    billableQuantity = od.Quantity;
                }
                
                subtotal += od.UnitPrice * billableQuantity;
            }
        }

        orderDto.Subtotal = subtotal;

        // Tính VAT (10%)
        orderDto.VatAmount = subtotal * 0.1m;

        // Tính phí dịch vụ (5%)
        orderDto.ServiceFee = subtotal * 0.05m;

        // Lấy discount từ Payment nếu có
        if (order.Payments != null && order.Payments.Any())
        {
            var latestPayment = order.Payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();
            if (latestPayment != null)
            {
                orderDto.DiscountAmount = latestPayment.DiscountAmount ?? 0;
            }
        }
        else
        {
            orderDto.DiscountAmount = 0;
        }

        // Tính tổng cộng
        orderDto.TotalAmount = subtotal + orderDto.VatAmount.Value + orderDto.ServiceFee.Value - orderDto.DiscountAmount.Value;
    }

    private static bool IsPendingStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && PendingStatuses.Contains(status);

    private static bool IsProcessedStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && ProcessedStatuses.Contains(status);

    private static void PopulateOrderMetadata(Order order, OrderDto orderDto)
    {
        if (order.Reservation != null && order.Reservation.ReservationTables != null && order.Reservation.ReservationTables.Any())
        {
            var tableNumbers = order.Reservation.ReservationTables
                .Where(rt => rt.Table != null && !string.IsNullOrWhiteSpace(rt.Table.TableNumber))
                .Select(rt => rt.Table.TableNumber!)
                .Distinct()
                .ToList();

            orderDto.TableNumbers = tableNumbers;
            orderDto.TableNumber = string.Join(", ", tableNumbers);
        }

        if (order.Customer?.User != null)
        {
            orderDto.CustomerName = order.Customer.User.FullName;
            orderDto.CustomerPhone = order.Customer.User.Phone;
            orderDto.CustomerEmail = order.Customer.User.Email;
        }

        if (order.Reservation?.Staff != null)
        {
            var staffName = order.Reservation.Staff.FullName;
            orderDto.StaffName = staffName;
            orderDto.WaiterName = staffName;
        }

        if (order.Transactions != null && order.Transactions.Any())
        {
            var latestPaidTransaction = order.Transactions
                .OrderByDescending(t => t.CompletedAt ?? t.CreatedAt)
                .FirstOrDefault(t =>
                    string.Equals(t.Status, "Success", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t.Status, "Paid", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase));

            if (latestPaidTransaction != null)
            {
                orderDto.PaidAt = latestPaidTransaction.CompletedAt ?? latestPaidTransaction.CreatedAt;
                orderDto.PaymentMethod = latestPaidTransaction.PaymentMethod;
            }
        }

        if (!string.IsNullOrWhiteSpace(orderDto.WaiterName) && string.IsNullOrWhiteSpace(orderDto.StaffName))
        {
            orderDto.StaffName = orderDto.WaiterName;
        }

        if (!string.IsNullOrWhiteSpace(orderDto.StaffName) && string.IsNullOrWhiteSpace(orderDto.WaiterName))
        {
            orderDto.WaiterName = orderDto.StaffName;
        }
    }

    public async Task<VietQRResponseDto> GenerateVietQRAsync(int orderId, string bankCode, string account, CancellationToken ct = default)
    {
        return await GenerateVietQRAsync(orderId, bankCode, account, null, ct);
    }

    public async Task<VietQRResponseDto> GenerateVietQRAsync(int orderId, string bankCode, string account, decimal? customAmount, CancellationToken ct = default)
    {
        var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(orderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {orderId}");
        }

        // Tính tổng tiền
        var orderDto = _mapper.Map<OrderDto>(order);
        CalculateOrderAmounts(order, orderDto);

        // Use custom amount if provided, otherwise use total amount
        var totalAmount = customAmount ?? orderDto.TotalAmount ?? 0;
        
        // Tạo mã đơn hàng (8 ký tự đầu của OrderId)
        var orderCode = $"RMS{orderId:D6}";
        
        // Tạo mô tả cho QR
        var description = customAmount.HasValue 
            ? $"Order#{orderCode} (Partial: {customAmount:N0} VND)"
            : $"Order#{orderCode}";
        
        // Encode description để URL-safe
        var encodedDescription = WebUtility.UrlEncode(description);
        
        // Tạo VietQR URL
        // Format: https://img.vietqr.io/image/{BANKCODE}-{ACCOUNT}.png?amount={AMOUNT}&addInfo={DESCRIPTION}
        var qrUrl = $"https://img.vietqr.io/image/{bankCode}-{account}-compact2.png?amount={(int)totalAmount}&addInfo={encodedDescription}";

        return new VietQRResponseDto
        {
            QrUrl = qrUrl,
            OrderId = orderId,
            Total = totalAmount,
            OrderCode = orderCode,
            Description = description
        };
    }

    // ========== PHASE 1: Payment Flow Extensions ==========

    /// <summary>
    /// CASE 1: Xử lý thanh toán tiền mặt với validation
    /// </summary>
    public async Task<TransactionDto> ProcessCashPaymentAsync(CashPaymentRequestDto request, int userId, CancellationToken ct = default)
    {
        // Lấy order và tính tổng tiền
        var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(request.OrderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {request.OrderId}");
        }

        var orderDto = _mapper.Map<OrderDto>(order);
        CalculateOrderAmounts(order, orderDto);
        var totalAmount = orderDto.TotalAmount ?? 0;

        // CASE 1A: Underpaid - Block confirmation
        if (request.AmountReceived < totalAmount)
        {
            await _auditLogService.LogEventAsync(
                "attempt_underpaid",
                "Order",
                request.OrderId,
                $"Số tiền nhận được ({request.AmountReceived:N0} VND) nhỏ hơn tổng tiền ({totalAmount:N0} VND)",
                null,
                userId,
                null,
                ct
            );

            throw new InvalidOperationException($"⚠️ Số tiền chưa đủ. Tổng tiền: {totalAmount:N0} VND, Nhận được: {request.AmountReceived:N0} VND. Vui lòng kiểm tra lại.");
        }

        // CASE 1B: Overpaid - Calculate refund
        decimal? refundAmount = null;
        if (request.AmountReceived > totalAmount)
        {
            refundAmount = request.AmountReceived - totalAmount;
            // Note: Frontend sẽ require "Đã trả lại tiền" confirmation
        }

        // Lock order trước khi thanh toán
        await LockOrderAsync(new OrderLockRequestDto { OrderId = request.OrderId }, userId, ct);

        try
        {
            // Tạo transaction
            var transaction = new Transaction
            {
                OrderId = request.OrderId,
                TransactionCode = $"TXN-{DateTime.UtcNow.Ticks}",
                Amount = totalAmount,
                AmountReceived = request.AmountReceived,
                RefundAmount = refundAmount,
                PaymentMethod = "Cash",
                Status = "Paid",
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                IsManualConfirmed = true,
                ConfirmedByUserId = userId,
                Notes = request.Notes ?? (refundAmount.HasValue ? $"Tiền thối lại: {refundAmount.Value:N0} VND" : null)
            };

            var savedTransaction = await _unitOfWork.Payments.SaveTransactionAsync(transaction);

            // Cập nhật trạng thái order
            order.Status = OrderStatusConstants.Paid;
            await _unitOfWork.Payments.UpdateAsync(order);
            //await _unitOfWork.SaveChangesAsync(ct);

            // Log success
            await _auditLogService.LogEventAsync(
                "payment_success",
                "Transaction",
                savedTransaction.TransactionId,
                $"Thanh toán tiền mặt thành công. Số tiền: {totalAmount:N0} VND" + (refundAmount.HasValue ? $", Tiền thối: {refundAmount.Value:N0} VND" : ""),
                null,
                userId,
                null,
                ct
            );

            // 🔓 GIẢI PHÓNG BÀN SAU KHI THANH TOÁN THÀNH CÔNG
            try
            {
                var tables = await _unitOfWork.Tables.GetTablesByOrderIdAsync(request.OrderId);
                if (tables != null && tables.Any())
                {
                    foreach (var table in tables)
                    {
                        table.Status = "Available";
                        await _unitOfWork.Tables.UpdateAsync(table);
                        
                        // Log table release
                        await _auditLogService.LogEventAsync(
                            eventType: "table_released",
                            entityType: "Table",
                            entityId: table.TableId,
                            description: $"Bàn {table.TableNumber} được giải phóng sau thanh toán tiền mặt cho Order {request.OrderId}",
                            userId: userId,
                            ct: ct
                        );
                    }
                    
                    await _unitOfWork.Tables.SaveAsync();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the payment - table release is secondary
                await _auditLogService.LogEventAsync(
                    eventType: "table_release_failed",
                    entityType: "Order",
                    entityId: request.OrderId,
                    description: $"Lỗi khi giải phóng bàn cho Order {request.OrderId}: {ex.Message}",
                    userId: userId,
                    ct: ct
                );
            }

            // Unlock order
            await UnlockOrderAsync(request.OrderId, ct);

            return _mapper.Map<TransactionDto>(savedTransaction);
        }
        catch
        {
            // Unlock order nếu có lỗi
            await UnlockOrderAsync(request.OrderId, ct);
            throw;
        }
    }

    /// <summary>
    /// CASE 2: Kiểm tra trạng thái thanh toán
    /// </summary>
    public async Task<PaymentStatusResponseDto> CheckPaymentStatusAsync(int orderId, CancellationToken ct = default)
    {
        var transactions = await _unitOfWork.Payments.GetTransactionsByOrderIdAsync(orderId);
        var latestTransaction = transactions.OrderByDescending(t => t.CreatedAt).FirstOrDefault();

        if (latestTransaction == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy giao dịch cho đơn hàng ID: {orderId}");
        }

        return new PaymentStatusResponseDto
        {
            TransactionId = latestTransaction.TransactionId,
            OrderId = latestTransaction.OrderId,
            Status = latestTransaction.Status,
            PaymentMethod = latestTransaction.PaymentMethod,
            Amount = latestTransaction.Amount,
            GatewayErrorCode = latestTransaction.GatewayErrorCode,
            GatewayErrorMessage = latestTransaction.GatewayErrorMessage,
            CreatedAt = latestTransaction.CreatedAt,
            CompletedAt = latestTransaction.CompletedAt,
            IsManualConfirmed = latestTransaction.IsManualConfirmed
        };
    }

    /// <summary>
    /// CASE 3: Retry payment đã thất bại
    /// </summary>
    public async Task<TransactionDto> RetryPaymentAsync(PaymentRetryRequestDto request, int userId, CancellationToken ct = default)
    {
        var transaction = await _unitOfWork.Payments.GetTransactionByIdAsync(request.TransactionId);
        if (transaction == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy giao dịch với ID: {request.TransactionId}");
        }

        // Increment retry count
        transaction.RetryCount++;
        transaction.LastRetryAt = DateTime.UtcNow;
        transaction.Status = "PaymentProcessing";
        transaction.GatewayErrorCode = null;
        transaction.GatewayErrorMessage = null;

        await _unitOfWork.Payments.UpdateTransactionAsync(transaction);

        // Log retry
        await _auditLogService.LogEventAsync(
            "payment_retry",
            "Transaction",
            transaction.TransactionId,
            $"Retry lần thứ {transaction.RetryCount}. Notes: {request.Notes}",
            null,
            userId,
            null,
            ct
        );

        return _mapper.Map<TransactionDto>(transaction);
    }

    /// <summary>
    /// CASE 4: Sync offline payments
    /// </summary>
    public async Task<List<TransactionDto>> SyncPaymentsAsync(List<int> transactionIds, CancellationToken ct = default)
    {
        var syncedTransactions = new List<TransactionDto>();

        foreach (var transactionId in transactionIds)
        {
            var transaction = await _unitOfWork.Payments.GetTransactionByIdAsync(transactionId);
            if (transaction != null)
            {
                syncedTransactions.Add(_mapper.Map<TransactionDto>(transaction));
            }
        }

        // Log sync
        await _auditLogService.LogEventAsync(
            "payment_sync",
            "Transaction",
            0,
            $"Sync {syncedTransactions.Count} transactions từ offline cache",
            System.Text.Json.JsonSerializer.Serialize(transactionIds),
            null,
            null,
            ct
        );

        return syncedTransactions;
    }

    /// <summary>
    /// CASE 5: Gateway callback notification
    /// </summary>
    public async Task<bool> NotifyPaymentAsync(PaymentNotifyRequestDto request, CancellationToken ct = default)
    {
        Transaction? transaction = null;

        // Tìm transaction theo SessionId hoặc TransactionCode
        if (!string.IsNullOrEmpty(request.SessionId))
        {
            transaction = await _unitOfWork.Payments.GetTransactionBySessionIdAsync(request.SessionId);
        }

        if (transaction == null && !string.IsNullOrEmpty(request.TransactionCode))
        {
            transaction = await _unitOfWork.Payments.GetTransactionByCodeAsync(request.TransactionCode);
        }

        if (transaction == null)
        {
            await _auditLogService.LogEventAsync(
                "payment_notify_failed",
                "Transaction",
                0,
                $"Không tìm thấy transaction với SessionId: {request.SessionId} hoặc TransactionCode: {request.TransactionCode}",
                System.Text.Json.JsonSerializer.Serialize(request),
                null,
                null,
                ct
            );
            return false;
        }

        // Update transaction status
        transaction.Status = request.Status;
        transaction.GatewayErrorCode = request.GatewayErrorCode;
        transaction.GatewayErrorMessage = request.GatewayErrorMessage;

        if (request.Status == "Paid" || request.Status == "Success")
        {
            transaction.Status = "Paid";
            transaction.CompletedAt = DateTime.UtcNow;

            // Update order status
            var order = await _unitOfWork.Payments.GetByIdAsync(transaction.OrderId);
            if (order != null)
            {
                order.Status = OrderStatusConstants.Paid;
                await _unitOfWork.Payments.UpdateAsync(order);
            }
        }
        else if (request.Status == "Failed" || request.Status == "Declined")
        {
            transaction.Status = "Failed";
        }

        await _unitOfWork.Payments.UpdateTransactionAsync(transaction);

        // Log notification
        await _auditLogService.LogEventAsync(
            "payment_notify",
            "Transaction",
            transaction.TransactionId,
            $"Gateway callback: {request.Status}",
            System.Text.Json.JsonSerializer.Serialize(request),
            null,
            null,
            ct
        );

        return true;
    }

    /// <summary>
    /// CASE 6: Lock order khi payment in progress
    /// </summary>
    public async Task<bool> LockOrderAsync(OrderLockRequestDto request, int userId, CancellationToken ct = default)
    {
        // Kiểm tra xem order đã bị lock chưa
        var existingLock = await _unitOfWork.OrderLocks.GetActiveLockAsync(request.OrderId);
        if (existingLock != null)
        {
            throw new InvalidOperationException($"Đơn hàng này đang được xử lý thanh toán bởi người dùng khác. Không thể thêm món.");
        }

        // Xóa các locks đã hết hạn
        await _unitOfWork.OrderLocks.RemoveExpiredLocksAsync();

        // Tạo lock mới (10 phút)
        var orderLock = new OrderLock
        {
            OrderId = request.OrderId,
            LockedByUserId = userId,
            SessionId = request.SessionId,
            Reason = request.Reason ?? "Payment in progress",
            LockedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        await _unitOfWork.OrderLocks.AddAsync(orderLock);
        //await _unitOfWork.SaveChangesAsync(ct);

        // Log lock
        await _auditLogService.LogEventAsync(
            "order_locked",
            "Order",
            request.OrderId,
            $"Order bị lock để xử lý thanh toán. Reason: {orderLock.Reason}",
            null,
            userId,
            null,
            ct
        );

        return true;
    }

    /// <summary>
    /// CASE 6: Unlock order
    /// </summary>
    public async Task<bool> UnlockOrderAsync(int orderId, CancellationToken ct = default)
    {
        await _unitOfWork.OrderLocks.RemoveLockAsync(orderId);

        // Log unlock
        await _auditLogService.LogEventAsync(
            "order_unlocked",
            "Order",
            orderId,
            "Order được unlock sau khi hoàn tất thanh toán",
            null,
            null,
            null,
            ct
        );

        return true;
    }

    /// <summary>
    /// CASE 6: Kiểm tra order có đang bị lock không
    /// </summary>
    public async Task<bool> IsOrderLockedAsync(int orderId, CancellationToken ct = default)
    {
        return await _unitOfWork.OrderLocks.IsOrderLockedAsync(orderId);
    }

    /// <summary>
    /// CASE 7: Xử lý split bill
    /// </summary>
    public async Task<List<TransactionDto>> ProcessSplitBillAsync(SplitBillRequestDto request, int userId, CancellationToken ct = default)
    {
        // Lấy order và tính tổng tiền
        var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(request.OrderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {request.OrderId}");
        }

        var orderDto = _mapper.Map<OrderDto>(order);
        CalculateOrderAmounts(order, orderDto);
        var totalAmount = orderDto.TotalAmount ?? 0;

        // Validate tổng các parts phải bằng totalAmount
        var partsTotal = request.Parts.Sum(p => p.Amount);
        if (Math.Abs(partsTotal - totalAmount) > 0.01m) // Cho phép sai số nhỏ do làm tròn
        {
            throw new InvalidOperationException($"Tổng các phần thanh toán ({partsTotal:N0} VND) không khớp với tổng đơn hàng ({totalAmount:N0} VND)");
        }

        // Lock order
        await LockOrderAsync(new OrderLockRequestDto { OrderId = request.OrderId }, userId, ct);

        try
        {
            var transactions = new List<Transaction>();

            // Tạo parent transaction (tổng)
            var parentTransaction = new Transaction
            {
                OrderId = request.OrderId,
                TransactionCode = $"TXN-SPLIT-{DateTime.UtcNow.Ticks}",
                Amount = totalAmount,
                PaymentMethod = "Split",
                Status = "PartiallyPaid",
                CreatedAt = DateTime.UtcNow,
                Notes = $"Split bill thành {request.Parts.Count} phần"
            };

            var savedParent = await _unitOfWork.Payments.SaveTransactionAsync(parentTransaction);

            // Tạo các child transactions
            foreach (var part in request.Parts)
            {
                var childTransaction = new Transaction
                {
                    OrderId = request.OrderId,
                    ParentTransactionId = savedParent.TransactionId,
                    TransactionCode = $"TXN-SPLIT-{DateTime.UtcNow.Ticks}-{part.GetHashCode()}",
                    Amount = part.Amount,
                    AmountReceived = part.AmountReceived,
                    PaymentMethod = part.PaymentMethod,
                    Status = part.PaymentMethod == "Cash" && part.AmountReceived.HasValue && part.AmountReceived.Value >= part.Amount
                        ? "Paid"
                        : "PaymentProcessing",
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = part.PaymentMethod == "Cash" && part.AmountReceived.HasValue && part.AmountReceived.Value >= part.Amount
                        ? DateTime.UtcNow
                        : null,
                    IsManualConfirmed = part.PaymentMethod == "Cash",
                    ConfirmedByUserId = part.PaymentMethod == "Cash" ? userId : null,
                    Notes = part.Notes
                };

                if (part.AmountReceived.HasValue && part.AmountReceived.Value > part.Amount)
                {
                    childTransaction.RefundAmount = part.AmountReceived.Value - part.Amount;
                }

                transactions.Add(childTransaction);
            }

            // Lưu tất cả child transactions
            foreach (var transaction in transactions)
            {
                await _unitOfWork.Payments.SaveTransactionAsync(transaction);
            }

            // Kiểm tra xem tất cả parts đã paid chưa
            var allPaid = transactions.All(t => t.Status == "Paid");
            if (allPaid)
            {
                savedParent.Status = "Paid";
                savedParent.CompletedAt = DateTime.UtcNow;
                order.Status = OrderStatusConstants.Paid;
            }
            else
            {
                order.Status = OrderStatusConstants.PartiallyPaid;
            }

            await _unitOfWork.Payments.UpdateTransactionAsync(savedParent);
            await _unitOfWork.Payments.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            // Log split bill
            await _auditLogService.LogEventAsync(
                "split_bill_processed",
                "Order",
                request.OrderId,
                $"Split bill thành {request.Parts.Count} phần. Tổng: {totalAmount:N0} VND",
                System.Text.Json.JsonSerializer.Serialize(request.Parts.Select(p => new { p.PaymentMethod, p.Amount })),
                userId,
                null,
                ct
            );

            // 🔓 GIẢI PHÓNG BÀN KHI BẮT ĐẦU SPLIT BILL
            try
            {
                var tables = await _unitOfWork.Tables.GetTablesByOrderIdAsync(request.OrderId);
                if (tables != null && tables.Any())
                {
                    foreach (var table in tables)
                    {
                        table.Status = "Available";
                        await _unitOfWork.Tables.UpdateAsync(table);
                        
                        // Log table release
                        await _auditLogService.LogEventAsync(
                            eventType: "table_released",
                            entityType: "Table",
                            entityId: table.TableId,
                            description: $"Bàn {table.TableNumber} được giải phóng khi bắt đầu split bill cho Order {request.OrderId}",
                            userId: userId,
                            ct: ct
                        );
                    }
                    
                    await _unitOfWork.Tables.SaveAsync();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the payment - table release is secondary
                await _auditLogService.LogEventAsync(
                    eventType: "table_release_failed",
                    entityType: "Order",
                    entityId: request.OrderId,
                    description: $"Lỗi khi giải phóng bàn cho Order {request.OrderId}: {ex.Message}",
                    userId: userId,
                    ct: ct
                );
            }

            // Unlock order
            await UnlockOrderAsync(request.OrderId, ct);

            // Return all transactions
            var allTransactions = new List<Transaction> { savedParent };
            allTransactions.AddRange(transactions);

            return allTransactions.Select(t => _mapper.Map<TransactionDto>(t)).ToList();
        }
        catch
        {
            // Unlock order nếu có lỗi
            await UnlockOrderAsync(request.OrderId, ct);
            throw;
        }
    }

    // ========== REVISED PAYMENT WORKFLOW METHODS ==========

    /// <summary>
    /// Bắt đầu thanh toán - tạo transaction và khởi tạo payment flow
    /// </summary>
    public async Task<TransactionDto> StartPaymentAsync(int orderId, string paymentMethod, CancellationToken ct = default)
    {
        // Validate order exists
        var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(orderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {orderId}");
        }

        // Check if order is already paid
        if (order.Status == "Paid")
        {
            throw new InvalidOperationException("Đơn hàng đã được thanh toán");
        }

        // Check if order is locked
        var isLocked = await IsOrderLockedAsync(orderId, ct);
        if (isLocked)
        {
            throw new InvalidOperationException("Đơn hàng đang được xử lý thanh toán bởi người khác");
        }

        // Calculate total amount
        var orderDto = _mapper.Map<OrderDto>(order);
        CalculateOrderAmounts(order, orderDto);
        var totalAmount = orderDto.TotalAmount ?? 0;

        if (totalAmount <= 0)
        {
            throw new InvalidOperationException("Tổng tiền đơn hàng phải lớn hơn 0");
        }

        // Lock order
        await LockOrderAsync(new OrderLockRequestDto { OrderId = orderId }, 0, ct); // userId = 0 for system

        try
        {
            // Generate transaction code
            var transactionCode = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{orderId}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            var sessionId = Guid.NewGuid().ToString();

            // Create transaction
            var transaction = new Transaction
            {
                OrderId = orderId,
                TransactionCode = transactionCode,
                Amount = totalAmount,
                PaymentMethod = paymentMethod,
                Status = paymentMethod == "Cash" ? "WaitingForPayment" : "PaymentProcessing",
                CreatedAt = DateTime.UtcNow,
                SessionId = sessionId,
                Notes = $"Bắt đầu thanh toán bằng {paymentMethod}"
            };

            // For QR payment, set status to WaitingForPayment (manual confirmation)
            // Simplified: No gateway integration, only manual confirmation
            if (paymentMethod == "QRBankTransfer" || paymentMethod == "QR")
            {
                transaction.Status = "WaitingForPayment"; // Will be confirmed manually
            }

            var savedTransaction = await _unitOfWork.Payments.SaveTransactionAsync(transaction);

            // Log audit
            await _auditLogService.LogEventAsync(
                eventType: "PaymentStarted",
                entityType: "Order",
                entityId: orderId,
                description: $"Bắt đầu thanh toán {paymentMethod} cho đơn hàng #{orderId}",
                userId: null,
                ct: ct
            );

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<TransactionDto>(savedTransaction);
        }
        catch
        {
            // Unlock order on error
            await UnlockOrderAsync(orderId, ct);
            throw;
        }
    }

    /// <summary>
    /// Xử lý callback từ payment gateway
    /// SIMPLIFIED: Not used for Cash/QR manual confirmation system
    /// Kept for backward compatibility but not actively used
    /// </summary>
    [Obsolete("Gateway callbacks not used in simplified Cash/QR payment system. Use ConfirmManualAsync instead.")]
    public async Task<bool> HandleCallbackAsync(PaymentNotifyRequestDto request, CancellationToken ct = default)
    {
        // Simplified payment system: Cash and QR use manual confirmation only
        // Gateway callbacks are not needed
        await _auditLogService.LogEventAsync(
            eventType: "PaymentCallbackIgnored",
            entityType: "Transaction",
            entityId: 0,
            description: $"Gateway callback received but ignored in simplified payment system. TransactionCode: {request.TransactionCode}",
            userId: null,
            ct: ct
        );
        return false;
    }

    /// <summary>
    /// Xác nhận thanh toán thủ công (cho cash hoặc khi gateway chậm)
    /// </summary>
    public async Task<TransactionDto> ConfirmManualAsync(PaymentConfirmRequestDto request, int userId, CancellationToken ct = default)
    {
        // Get transaction
        var transaction = await _unitOfWork.Payments.GetTransactionByIdAsync(request.TransactionId);
        if (transaction == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy giao dịch với ID: {request.TransactionId}");
        }

        if (transaction.OrderId != request.OrderId)
        {
            throw new InvalidOperationException("Transaction không thuộc về order này");
        }

        // Validate amount for cash payment
        if (transaction.PaymentMethod == "Cash")
        {
            if (!request.CashGiven.HasValue)
            {
                throw new InvalidOperationException("Vui lòng nhập số tiền khách đưa");
            }

            if (request.CashGiven.Value < transaction.Amount)
            {
                throw new InvalidOperationException($"Số tiền chưa đủ. Cần: {transaction.Amount:N0} VND, Nhận: {request.CashGiven.Value:N0} VND");
            }

            transaction.AmountReceived = request.CashGiven.Value;
            transaction.RefundAmount = request.CashGiven.Value - transaction.Amount;
        }

        // Update transaction
        transaction.Status = "Paid";
        transaction.CompletedAt = DateTime.UtcNow;
        transaction.IsManualConfirmed = true;
        transaction.ConfirmedByUserId = userId;
        transaction.GatewayReference = request.GatewayReference ?? transaction.GatewayReference;
        transaction.Notes = request.Notes ?? transaction.Notes;

        await _unitOfWork.Payments.UpdateTransactionAsync(transaction);

        // Update order status
        await _unitOfWork.Payments.UpdateOrderStatusAsync(request.OrderId, "Paid");

        // Log audit
        await _auditLogService.LogEventAsync(
            eventType: "PaymentConfirmed",
            entityType: "Order",
            entityId: request.OrderId,
            description: $"Xác nhận thanh toán thủ công bởi user {userId}. Transaction: {transaction.TransactionCode}",
            userId: userId,
            ct: ct
        );

        await _unitOfWork.SaveChangesAsync();

        // Trigger post-payment actions
        await TriggerPostPaymentActionsAsync(request.OrderId, transaction.TransactionId, ct);

        // 🔓 GIẢI PHÓNG BÀN SAU KHI XÁC NHẬN THANH TOÁN THÀNH CÔNG
        try
        {
            var tables = await _unitOfWork.Tables.GetTablesByOrderIdAsync(request.OrderId);
            if (tables != null && tables.Any())
            {
                foreach (var table in tables)
                {
                    table.Status = "Available";
                    await _unitOfWork.Tables.UpdateAsync(table);
                    
                    // Log table release
                    await _auditLogService.LogEventAsync(
                        eventType: "table_released",
                        entityType: "Table",
                        entityId: table.TableId,
                        description: $"Bàn {table.TableNumber} được giải phóng sau xác nhận thanh toán cho Order {request.OrderId}",
                        userId: userId,
                        ct: ct
                    );
                }
                
                await _unitOfWork.Tables.SaveAsync();
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the payment - table release is secondary
            await _auditLogService.LogEventAsync(
                eventType: "table_release_failed",
                entityType: "Order",
                entityId: request.OrderId,
                description: $"Lỗi khi giải phóng bàn cho Order {request.OrderId}: {ex.Message}",
                userId: userId,
                ct: ct
            );
        }

        // Unlock order
        await UnlockOrderAsync(request.OrderId, ct);

        return _mapper.Map<TransactionDto>(transaction);
    }

    /// <summary>
    /// Hủy thanh toán
    /// </summary>
    public async Task<bool> CancelPaymentAsync(PaymentCancelRequestDto request, int userId, CancellationToken ct = default)
    {
        // Get transaction
        var transaction = await _unitOfWork.Payments.GetTransactionByIdAsync(request.TransactionId);
        if (transaction == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy giao dịch với ID: {request.TransactionId}");
        }

        if (transaction.OrderId != request.OrderId)
        {
            throw new InvalidOperationException("Transaction không thuộc về order này");
        }

        // Only allow cancel if status is Pending or Processing
        if (transaction.Status != "WaitingForPayment" && transaction.Status != "PaymentProcessing")
        {
            throw new InvalidOperationException($"Không thể hủy giao dịch với trạng thái: {transaction.Status}");
        }

        // Update transaction
        transaction.Status = "Cancelled";
        transaction.Notes = string.IsNullOrEmpty(request.Reason) 
            ? $"Hủy bởi user {userId}" 
            : $"Hủy bởi user {userId}: {request.Reason}";

        await _unitOfWork.Payments.UpdateTransactionAsync(transaction);

        // Log audit
        await _auditLogService.LogEventAsync(
            eventType: "PaymentCancelled",
            entityType: "Order",
            entityId: request.OrderId,
            description: $"Hủy thanh toán. Lý do: {request.Reason ?? "Không có"}",
            userId: userId,
            ct: ct
        );

        await _unitOfWork.SaveChangesAsync();

        // Unlock order
        await UnlockOrderAsync(request.OrderId, ct);

        return true;
    }

    /// <summary>
    /// Retry các transaction đang pending (background job)
    /// </summary>
    public async Task<List<TransactionDto>> RetryPendingTransactionsAsync(CancellationToken ct = default)
    {
        // Get all pending transactions older than 5 minutes
        var cutoffTime = DateTime.UtcNow.AddMinutes(-5);
        
        // Note: This requires a repository method to get pending transactions
        // For now, we'll get transactions by order and filter
        // TODO: Add GetPendingTransactionsAsync to repository
        
        var retriedTransactions = new List<TransactionDto>();

        // This is a simplified implementation
        // In production, you'd want a proper query to get pending transactions
        // For now, return empty list as placeholder
        // TODO: Implement proper retry logic with gateway API calls

        return retriedTransactions;
    }

    /// <summary>
    /// Step 8: Trigger post-payment actions (inventory, reports, revenue, WebSocket events)
    /// </summary>
    private async Task TriggerPostPaymentActionsAsync(int orderId, int transactionId, CancellationToken ct = default)
    {
        try
        {
            // Step 8.1: Get order to record revenue
            var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(orderId);
            if (order != null && order.TotalAmount.HasValue)
            {
                // Step 8.2: Record revenue (placeholder - implement RevenueService if needed)
                // await _revenueService.RecordAsync(orderId, order.TotalAmount.Value, transactionId, ct);
                
                // Log revenue recording
                await _auditLogService.LogEventAsync(
                    eventType: "RevenueRecorded",
                    entityType: "Order",
                    entityId: orderId,
                    description: $"Đã ghi nhận doanh thu: {order.TotalAmount.Value:N0} VND cho order {orderId}",
                    userId: null,
                    ct: ct
                );
            }

            // Step 8.3: Inventory deduction (placeholder)
            // await _inventoryService.DeductItemsAsync(orderId, ct);

            // Step 8.4: Update reports (placeholder)
            // await _reportService.SyncPaymentAsync(orderId, transactionId, ct);

            // Step 8.5: Generate PDF receipt
            try
            {
                var receiptService = _serviceProvider.GetRequiredService<IReceiptService>();
                var receiptUrl = await receiptService.GenerateReceiptPdfAsync(orderId, ct);
                
                await _auditLogService.LogEventAsync(
                    eventType: "ReceiptGenerated",
                    entityType: "Order",
                    entityId: orderId,
                    description: $"Đã tạo hóa đơn PDF: {receiptUrl}",
                    userId: null,
                    ct: ct
                );
            }
            catch (Exception receiptEx)
            {
                // Log receipt generation error but don't fail payment
                await _auditLogService.LogEventAsync(
                    eventType: "ReceiptGenerationError",
                    entityType: "Order",
                    entityId: orderId,
                    description: $"Lỗi khi tạo hóa đơn: {receiptEx.Message}",
                    userId: null,
                    ct: ct
                );
            }

            // Step 8.6: Emit WebSocket event for real-time updates
            // await _hubContext.Clients.All.SendAsync("ORDER_PAID", new { orderId, transactionId, amount = order?.TotalAmount }, ct);

            // Log audit
            await _auditLogService.LogEventAsync(
                eventType: "PostPaymentActions",
                entityType: "Order",
                entityId: orderId,
                description: $"Đã trigger post-payment actions cho order {orderId}",
                userId: null,
                ct: ct
            );
        }
        catch (Exception ex)
        {
            // Log error but don't fail the payment
            await _auditLogService.LogEventAsync(
                eventType: "PostPaymentActionsError",
                entityType: "Order",
                entityId: orderId,
                description: $"Lỗi khi trigger post-payment actions: {ex.Message}",
                userId: null,
                ct: ct
            );
        }
    }
}

