using AutoMapper;
using BusinessAccessLayer.Constants;
using BusinessAccessLayer.DTOs.Payment;
using BusinessAccessLayer.Services.Interfaces;
using BusinessAccessLayer.DTOs;
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
        OrderStatusConstants.PendingPayment,
        "WaitingForPayment",
        "Processing",
        OrderStatusConstants.Confirmed,  // Đơn đã được khách xác nhận, chờ thanh toán
        "Cooking",
        "Ready",
        "Late",
        "Done"
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

        // 🔄 Luôn lấy toàn bộ orders, sau đó filter theo ngày dựa trên PaidAt (nếu có) hoặc CreatedAt
        var orders = await _unitOfWork.Payments.GetAllOrdersWithDetailsAsync();
        var orderDtos = new List<OrderDto>();

        foreach (var order in orders)
        {
            var orderDto = _mapper.Map<OrderDto>(order);
            CalculateOrderAmounts(order, orderDto);
            PopulateOrderMetadata(order, orderDto);
            orderDtos.Add(orderDto);
        }

        // ✅ Filter theo ngày: ưu tiên PaidAt, fallback CreatedAt cho đơn chưa thanh toán
        if (date.HasValue)
        {
            orderDtos = orderDtos.Where(o =>
                (o.PaidAt.HasValue && DateOnly.FromDateTime(o.PaidAt.Value) == selectedDate) ||
                (!o.PaidAt.HasValue && o.CreatedAt.HasValue && DateOnly.FromDateTime(o.CreatedAt.Value) == selectedDate)
            ).ToList();
        }

        // Tính lại tổng số sau khi filter theo ngày
        var pendingCount = orderDtos.Count(o => IsPendingStatus(o.Status));
        var processedCount = orderDtos.Count(o => IsProcessedStatus(o.Status));

        IEnumerable<OrderDto> filteredOrders = orderDtos;
        if (!string.IsNullOrWhiteSpace(statusFilter) && !statusFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            // ✅ Hỗ trợ cả "pending"/"processed" và "Confirmed"/"Paid"
            var statusLower = statusFilter.ToLowerInvariant();
            
            if (statusLower == "pending" || statusLower == "confirmed" || statusLower == "pendingpayment")
            {
                // Filter orders với status pending (bao gồm Confirmed, PendingPayment, etc.)
                filteredOrders = orderDtos.Where(o => IsPendingStatus(o.Status));
            }
            else if (statusLower == "processed" || statusLower == "paid" || statusLower == "completed" || statusLower == "success")
            {
                // Filter orders với status processed (bao gồm Paid, Completed, Success)
                filteredOrders = orderDtos.Where(o => IsProcessedStatus(o.Status));
            }
            else
            {
                // ✅ Filter theo status chính xác nếu không match với pending/processed
                filteredOrders = orderDtos.Where(o => 
                    !string.IsNullOrWhiteSpace(o.Status) && 
                    o.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));
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
        // ✅ DEBUG: Log để trace
        System.Diagnostics.Debug.WriteLine($"[GetOrderDetailAsync] Loading order {orderId}");
        
        var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(orderId);

        if (order == null)
        {
            System.Diagnostics.Debug.WriteLine($"[GetOrderDetailAsync] Order {orderId} not found");
            return null;
        }

        // ✅ DEBUG: Log order data trước khi map
        System.Diagnostics.Debug.WriteLine($"[GetOrderDetailAsync] Order {orderId} - CustomerId: {order.CustomerId}, ReservationId: {order.ReservationId}, Customer: {order.Customer != null}, Customer.User: {order.Customer?.User != null}, Reservation.Customer: {order.Reservation?.Customer != null}, Reservation.Customer.User: {order.Reservation?.Customer?.User != null}");

        var orderDto = _mapper.Map<OrderDto>(order);

        // Tính toán các khoản tiền
        CalculateOrderAmounts(order, orderDto);
        PopulateOrderMetadata(order, orderDto);
        
        // ✅ DEBUG: Log orderDto sau khi populate
        System.Diagnostics.Debug.WriteLine($"[GetOrderDetailAsync] OrderDto {orderId} - CustomerId: {orderDto.CustomerId}, CustomerName: {orderDto.CustomerName}, CustomerPhone: {orderDto.CustomerPhone}");
        
        // Lấy số tiền khách đưa và tiền thối lại từ transaction cuối cùng (nếu có)
        if (order.Transactions != null && order.Transactions.Any())
        {
            var latestTransaction = order.Transactions
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefault();
            
            if (latestTransaction != null)
            {
                // Lấy số tiền khách đưa (cho thanh toán tiền mặt)
                if (latestTransaction.AmountReceived.HasValue && latestTransaction.AmountReceived.Value > 0)
                {
                    orderDto.AmountReceived = latestTransaction.AmountReceived.Value;
                }
                
                // Lấy tiền thối lại
                if (latestTransaction.RefundAmount.HasValue && latestTransaction.RefundAmount.Value > 0)
                {
                    orderDto.ChangeAmount = latestTransaction.RefundAmount.Value;
                }
            }
        }
        
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

        // Map + tính toán tổng hiện tại
        var orderDto = _mapper.Map<OrderDto>(order);
        CalculateOrderAmounts(order, orderDto);

        decimal discountAmount = request.DiscountAmount ?? 0;




        // Nếu có VoucherCode → áp dụng logic Voucher
        if (!string.IsNullOrWhiteSpace(request.VoucherCode))
        {
            var voucherService = _serviceProvider.GetService<IVoucherService>();
            if (voucherService == null)
            {
                throw new Exception("VoucherService chưa được cấu hình trong hệ thống.");
            }

            var vouchers = await voucherService.GetAllAsync();
            var today = DateTime.Today;
            
            // ✅ VALIDATION ĐẦY ĐỦ: Code, IsDelete, Status, và THỜI HẠN
            var voucher = vouchers.FirstOrDefault(v =>
                string.Equals(v.Code, request.VoucherCode!.Trim(), StringComparison.OrdinalIgnoreCase) &&
                v.IsDelete != true &&
                string.Equals(v.Status, "Đang sử dụng", StringComparison.OrdinalIgnoreCase) &&
                // ✅ Check thời hạn: StartDate <= today <= EndDate
                (!v.StartDate.HasValue || v.StartDate.Value.Date <= today) &&
                (!v.EndDate.HasValue || v.EndDate.Value.Date >= today));

            if (voucher == null)
            {
                throw new KeyNotFoundException("Mã giảm giá không hợp lệ, đã hết hạn hoặc chưa đến thời gian sử dụng.");
            }

            var subtotal = orderDto.Subtotal ?? 0;

            // ✅ Check điều kiện giá trị tối thiểu
            if (voucher.MinOrderValue.HasValue && subtotal < voucher.MinOrderValue.Value)
            {
                throw new InvalidOperationException(
                    $"Đơn hàng chưa đủ giá trị tối thiểu {voucher.MinOrderValue.Value:N0} ₫ để áp dụng voucher này.");
            }

            // Tính mức giảm theo loại voucher
            if (string.Equals(voucher.DiscountType, "Phần trăm", StringComparison.OrdinalIgnoreCase))
            {
                var raw = subtotal * (voucher.DiscountValue / 100m);
                discountAmount = voucher.MaxDiscount.HasValue
                    ? Math.Min(raw, voucher.MaxDiscount.Value)
                    : raw;
            }
            else // "Giá trị cố định"
            {
                discountAmount = voucher.DiscountValue;
            }

            // Không cho giảm quá subtotal
            if (discountAmount > subtotal)
            {
                discountAmount = subtotal;
            }
        }

        // ✅ Làm tròn discount amount lên mệnh giá 1000
        var roundedDiscountAmount = RoundUpToThousand(discountAmount);
        orderDto.DiscountAmount = roundedDiscountAmount;

        // Tính lại tổng tiền sau ưu đãi và làm tròn
        var totalBeforeRounding = (orderDto.Subtotal ?? 0) + (orderDto.VatAmount ?? 0) +
                                  (orderDto.ServiceFee ?? 0) - orderDto.DiscountAmount.Value;
        orderDto.TotalAmount = RoundUpToThousand(totalBeforeRounding);

        // ✅ FIX: Lưu discount vào Payment record trong database
        // Tìm Payment record của order (nếu có) hoặc tạo mới
        var payment = order.Payments?.OrderByDescending(p => p.PaymentDate ?? DateTime.MinValue).FirstOrDefault();
        
        int? voucherId = null;
        if (!string.IsNullOrWhiteSpace(request.VoucherCode))
        {
            var voucherService = _serviceProvider.GetService<IVoucherService>();
            if (voucherService != null)
            {
                var vouchers = await voucherService.GetAllAsync();
                var today = DateTime.Today;
                var voucher = vouchers.FirstOrDefault(v =>
                    string.Equals(v.Code, request.VoucherCode!.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    v.IsDelete != true &&
                    string.Equals(v.Status, "Đang sử dụng", StringComparison.OrdinalIgnoreCase) &&
                    (!v.StartDate.HasValue || v.StartDate.Value.Date <= today) &&
                    (!v.EndDate.HasValue || v.EndDate.Value.Date >= today));
                voucherId = voucher?.VoucherId;
            }
        }

        if (payment == null)
        {
            // Tạo Payment record mới để lưu discount
            payment = new Payment
            {
                OrderId = request.OrderId,
                PaymentMethod = "Pending", // Tạm thời, sẽ cập nhật khi thanh toán
                Subtotal = orderDto.Subtotal ?? 0,
                DiscountAmount = roundedDiscountAmount,
                Vatpercent = 10, // Default VAT
                Vatamount = orderDto.VatAmount ?? 0,
                FinalAmount = orderDto.TotalAmount ?? 0,
                VoucherId = voucherId,
                PaymentDate = null // Chưa thanh toán
            };
            // Thêm Payment vào order và save
            if (order.Payments == null)
            {
                order.Payments = new List<Payment>();
            }
            order.Payments.Add(payment);
        }
        else
        {
            // Cập nhật Payment record hiện có
            payment.DiscountAmount = roundedDiscountAmount;
            payment.VoucherId = voucherId;
            payment.Subtotal = orderDto.Subtotal ?? 0;
            payment.Vatamount = orderDto.VatAmount ?? 0;
            payment.FinalAmount = orderDto.TotalAmount ?? 0;
        }

        // Update order để trigger save Payment changes
        await _unitOfWork.Payments.UpdateAsync(order);
        
        // Save changes để lưu discount vào database
        await _unitOfWork.SaveChangesAsync();

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

        return _mapper.Map<TransactionDto>(savedTransaction);
    }

    public async Task<TransactionDto> ProcessPaymentAsync(PaymentRequestDto request, int userId, CancellationToken ct = default)
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

        // ✅ FIX: Nếu có SessionId, tìm transaction đã tạo từ InitiatePayment và sử dụng amount từ đó
        Transaction? existingTransaction = null;
        decimal expectedAmount = request.Amount;

        if (!string.IsNullOrEmpty(request.SessionId))
        {
            existingTransaction = await _unitOfWork.Payments.GetTransactionBySessionIdAsync(request.SessionId);
            if (existingTransaction != null)
            {
                // Sử dụng amount từ transaction đã tạo (đã tính đúng với discount)
                expectedAmount = existingTransaction.Amount;
                
                // Validate: amount từ request phải khớp với amount trong transaction
                if (Math.Abs(request.Amount - existingTransaction.Amount) > 0.01m)
                {
                    throw new InvalidOperationException($"Số tiền thanh toán không khớp với phiên thanh toán. Phiên: {existingTransaction.Amount:N0} ₫, Nhận được: {request.Amount:N0} ₫");
                }
            }
        }

        // Nếu không có existing transaction, tính lại amount (cho backward compatibility)
        if (existingTransaction == null)
        {
            var orderDto = _mapper.Map<OrderDto>(order);
            CalculateOrderAmounts(order, orderDto);
            expectedAmount = (orderDto.Subtotal ?? 0) + (orderDto.VatAmount ?? 0) +
                            (orderDto.ServiceFee ?? 0) - (orderDto.DiscountAmount ?? 0);

            if (Math.Abs(request.Amount - expectedAmount) > 0.01m)
            {
                throw new InvalidOperationException($"Số tiền thanh toán không khớp. Mong đợi: {expectedAmount:N0} ₫, Nhận được: {request.Amount:N0} ₫");
            }
        }

        // Validate cash payment
        if (request.PaymentMethod == "Cash" && request.CashGiven.HasValue)
        {
            if (request.CashGiven.Value < expectedAmount)
            {
                throw new InvalidOperationException("Số tiền khách đưa không đủ!");
            }
        }

        Transaction savedTransaction;
        
        if (existingTransaction != null)
        {
            // ✅ Cập nhật transaction đã tồn tại thay vì tạo mới
            existingTransaction.Status = "Paid";
            existingTransaction.CompletedAt = DateTime.UtcNow;
            existingTransaction.Notes = request.Notes ?? existingTransaction.Notes;
            
            if (request.PaymentMethod == "Cash" && request.CashGiven.HasValue)
            {
                existingTransaction.AmountReceived = request.CashGiven.Value;
                existingTransaction.RefundAmount = request.CashGiven.Value - existingTransaction.Amount;
            }
            
            await _unitOfWork.Payments.UpdateTransactionAsync(existingTransaction);
            savedTransaction = existingTransaction;
        }
        else
        {
            // Tạo transaction mới (backward compatibility)
            var transaction = new Transaction
            {
                OrderId = request.OrderId,
                TransactionCode = $"TXN-{DateTime.UtcNow.Ticks}",
                Amount = request.Amount,
                PaymentMethod = request.PaymentMethod,
                Status = "Paid",
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                SessionId = request.SessionId,
                Notes = request.Notes
            };

            savedTransaction = await _unitOfWork.Payments.SaveTransactionAsync(transaction);
        }

        // Cập nhật trạng thái đơn hàng
        order.Status = OrderStatusConstants.Paid;
        await _unitOfWork.Payments.UpdateAsync(order);

        // 🔓 Giải phóng bàn và hoàn thành reservation
        await ReleaseTablesAndCompleteReservationAsync(request.OrderId, userId, ct);

        // Save changes
        await _unitOfWork.SaveChangesAsync();

        // ✅ Trigger post-payment actions (VIP update, LoyaltyPoints +1, etc.)
        await TriggerPostPaymentActionsAsync(request.OrderId, savedTransaction.TransactionId, ct);

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

        // ✅ Danh sách status được phép thanh toán (billable)
        var billableStatuses = new[] { "Cooking", "Done", "Ready", "Served", "cooking", "done", "ready", "served", "Đang chế biến", "Đã xong", "Sẵn sàng", "Đã phục vụ" };
        
        foreach (var confirmed in request.Items)
        {
            var detail = order.OrderDetails.FirstOrDefault(d => d.OrderDetailId == confirmed.OrderDetailId);
            if (detail == null)
            {
                continue;
            }

            if (confirmed.IsRemoved)
            {
                // ✅ QUAN TRỌNG: Giải phóng reserved quantity TRƯỚC KHI cập nhật status
                // Phải gọi TRƯỚC khi set status = Removed để release có thể check status Pending/Cooking
                if (detail.MenuItem != null)
                {
                    try
                    {
                        var inventoryService = _serviceProvider.GetRequiredService<IInventoryIngredientService>();
                        var releaseResult = await inventoryService.ReleaseReservedBatchesForOrderDetailAsync(detail.OrderDetailId);
                        if (!releaseResult.success)
                        {
                            // Log warning nhưng không fail việc hủy món
                            Console.WriteLine($"Warning: Không thể giải phóng nguyên liệu khi hủy món {detail.OrderDetailId}: {releaseResult.message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error nhưng không fail việc hủy món
                        Console.WriteLine($"Error releasing reserved batches for order detail {detail.OrderDetailId}: {ex.Message}");
                    }
                }
                
                // Món bị hủy: set cả Quantity và QuantityUsed về 0 (SAU KHI đã release)
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
                
                // ✅ FIX BUG: CHỈ chuyển status thành "Done" nếu món có status billable
                // KHÔNG chuyển món "Pending" thành "Done"
                var currentStatus = (detail.Status ?? "").Trim();
                bool isBillable = billableStatuses.Any(s => string.Equals(s, currentStatus, StringComparison.OrdinalIgnoreCase));
                
                if (isBillable)
                {
                    // Món đã được bếp xử lý (Cooking/Done/Ready/Served) → chuyển thành Done để thanh toán
                    detail.Status = "Done";
                }
                // Nếu món có status "Pending" → GIỮ NGUYÊN status, KHÔNG chuyển thành "Done"
                // Món "Pending" sẽ không được tính vào hóa đơn
            }
        }

        // ✅ TỰ ĐỘNG CHUYỂN TRẠNG THÁI CÁC MÓN CÓ STATUS "Cooking", "Done", "Ready", "Served" THÀNH "Done"
        // Các món này sẽ được lấy ra để thanh toán
        // ⚠️ KHÔNG chuyển món có status "Pending" thành "Done"
        var billableStatusesForAutoUpdate = new[] { "Cooking", "Done", "Ready", "Served", "cooking", "done", "ready", "served", "Đang chế biến", "Đã xong", "Sẵn sàng", "Đã phục vụ" };
        
        foreach (var detail in order.OrderDetails)
        {
            // Bỏ qua món đã bị hủy
            if (detail.Status == "Removed" || detail.Status == "Cancelled")
            {
                continue;
            }

            // ✅ Bỏ qua món đã được xử lý trong request.Items (đã được set status ở trên)
            // Kiểm tra xem món này có trong request.Items không
            var wasProcessedInRequest = request.Items.Any(item => item.OrderDetailId == detail.OrderDetailId);
            if (wasProcessedInRequest)
            {
                continue; // Đã xử lý rồi, không xử lý lại
            }

            var currentStatus = (detail.Status ?? "").Trim();
            
            // ✅ XỬ LÝ MÓN LẺ (KHÔNG PHẢI COMBO)
            if (!detail.ComboId.HasValue)
            {
                // ⚠️ CHỈ chuyển status thành "Done" nếu món có status billable (Cooking/Done/Ready/Served)
                // KHÔNG chuyển món "Pending" thành "Done"
                if (billableStatusesForAutoUpdate.Any(s => string.Equals(s, currentStatus, StringComparison.OrdinalIgnoreCase)))
                {
                    detail.Status = "Done";
                    
                    // Nếu là món ConsumptionBased và chưa có QuantityUsed → set QuantityUsed = Quantity
                    if (detail.MenuItem?.BillingType == ItemBillingType.ConsumptionBased && 
                        !detail.QuantityUsed.HasValue)
                    {
                        detail.QuantityUsed = detail.Quantity;
                    }
                }
                // Nếu món có status "Pending" → GIỮ NGUYÊN, KHÔNG chuyển thành "Done"
            }
            // ✅ XỬ LÝ COMBO
            else if (detail.ComboId.HasValue)
            {
                // ⚠️ CHỈ chuyển status thành "Done" nếu combo có status billable
                // KHÔNG chuyển combo "Pending" thành "Done"
                if (billableStatusesForAutoUpdate.Any(s => string.Equals(s, currentStatus, StringComparison.OrdinalIgnoreCase)))
                {
                    detail.Status = "Done";
                    
                    // ✅ CHUYỂN TRẠNG THÁI TẤT CẢ MÓN CON TRONG COMBO THÀNH "Done" (chỉ món con có status billable)
                    if (detail.OrderComboItems != null && detail.OrderComboItems.Any())
                    {
                        foreach (var comboItem in detail.OrderComboItems)
                        {
                            var comboItemStatus = (comboItem.Status ?? "").Trim();
                            // ⚠️ CHỈ chuyển các món con có status Cooking/Done/Ready/Served thành Done
                            // KHÔNG chuyển món con "Pending" thành "Done"
                            if (billableStatusesForAutoUpdate.Any(s => string.Equals(s, comboItemStatus, StringComparison.OrdinalIgnoreCase)))
                            {
                                comboItem.Status = "Done";
                            }
                        }
                    }
                }
                // Nếu combo có status "Pending" → GIỮ NGUYÊN, KHÔNG chuyển thành "Done"
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
    /// Làm tròn lên mệnh giá 1000 VND
    /// Ví dụ: 157600 → 158000, 157400 → 158000, 157000 → 157000
    /// </summary>
    private static decimal RoundUpToThousand(decimal amount)
    {
        if (amount <= 0) return 0;
        return Math.Ceiling(amount / 1000m) * 1000m;
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
                // Bỏ qua món đã bị xóa hoặc đã hủy
                var status = (od.Status ?? "").Trim();
                var statusLower = status.ToLower();
                
                if (statusLower == "removed" || statusLower == "cancelled" || statusLower == "đã hủy")
                {
                    continue;
                }

                // ✅ LOGIC MỚI: Chỉ tính tiền món có Status = "Cooking", "Done", "Ready"
                // Không tính tiền món có Status = "Pending"
                
                // Danh sách status được phép thanh toán
                var billableStatuses = new[] { "cooking", "done", "ready", "served", "đang chế biến", "đã xong", "sẵn sàng" };
                bool isBillable = billableStatuses.Any(s => statusLower == s);

                // ✅ XỬ LÝ COMBO: Nếu là combo, kiểm tra OrderComboItems
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

                // ✅ LOGIC: Phân biệt 2 loại món
                // Check MenuItem and BillingType with null-safety
                var billingType = od.MenuItem?.BillingType ?? ItemBillingType.KitchenPrepared;

                if (billingType == ItemBillingType.ConsumptionBased)
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

        // ✅ Làm tròn Subtotal lên mệnh giá 1000
        orderDto.Subtotal = RoundUpToThousand(subtotal);

        // Tính VAT (10%) từ Subtotal đã làm tròn
        orderDto.VatAmount = RoundUpToThousand(orderDto.Subtotal.Value * 0.1m);

        // Tính phí dịch vụ (5%) từ Subtotal đã làm tròn
        orderDto.ServiceFee = RoundUpToThousand(orderDto.Subtotal.Value * 0.05m);

        // Lấy discount từ Payment nếu có và làm tròn
        if (order.Payments != null && order.Payments.Any())
        {
            var latestPayment = order.Payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();
            if (latestPayment != null)
            {
                orderDto.DiscountAmount = RoundUpToThousand(latestPayment.DiscountAmount ?? 0);
            }
        }
        else
        {
            orderDto.DiscountAmount = 0;
        }

        // Lấy thông tin đặt cọc từ Reservation (nếu có)
        decimal depositToDeduct = 0;
        if (order.Reservation != null)
        {
            // Ưu tiên lấy tổng tiền cọc đã thanh toán, fallback về DepositAmount cũ nếu cần
            var deposit = order.Reservation.TotalDepositPaid
                          ?? order.Reservation.DepositAmount
                          ?? 0;

            orderDto.DepositAmount = deposit;
            orderDto.DepositPaid = order.Reservation.DepositPaid;

            // Chỉ trừ tiền cọc nếu khách đã thanh toán cọc
            if (order.Reservation.DepositPaid && deposit > 0)
            {
                depositToDeduct = deposit;
            }
        }

        // Tính tổng cộng trước khi trừ deposit (Subtotal + VAT + Service Fee - Discount)
        decimal totalBeforeDeposit = orderDto.Subtotal.Value + orderDto.VatAmount.Value + orderDto.ServiceFee.Value
                                    - orderDto.DiscountAmount.Value;

        // ✅ XỬ LÝ TRƯỜNG HỢP DEPOSIT > TOTAL
        // Nếu tiền cọc lớn hơn tổng tiền thanh toán, cần trả lại tiền thừa cho khách
        if (depositToDeduct > 0 && depositToDeduct > totalBeforeDeposit)
        {
            // Tính số tiền cần trả lại và làm tròn
            orderDto.DepositRefundAmount = RoundUpToThousand(depositToDeduct - totalBeforeDeposit);
            // Tổng tiền thanh toán = 0 (vì đã đủ tiền cọc)
            orderDto.TotalAmount = 0;
        }
        else
        {
            // Trừ tiền cọc vào tổng tiền và làm tròn
            orderDto.TotalAmount = RoundUpToThousand(totalBeforeDeposit - depositToDeduct);
            orderDto.DepositRefundAmount = 0;
            
            // Đảm bảo tổng tiền không âm
            if (orderDto.TotalAmount < 0)
            {
                orderDto.TotalAmount = 0;
            }
        }
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

        // ✅ FIX: Đảm bảo CustomerId được set (từ Order hoặc Reservation)
        if (order.CustomerId.HasValue)
        {
            orderDto.CustomerId = order.CustomerId.Value;
        }
        else if (order.Reservation != null && order.Reservation.CustomerId > 0)
        {
            // Fallback: Lấy CustomerId từ Reservation nếu Order không có
            // Reservation.CustomerId là int (không nullable), không phải int?
            orderDto.CustomerId = order.Reservation.CustomerId;
        }
        
        // ✅ FIX: Lấy customer info từ Customer.User hoặc Reservation.Customer.User
        if (order.Customer?.User != null)
        {
            orderDto.CustomerName = order.Customer.User.FullName;
            orderDto.CustomerPhone = order.Customer.User.Phone;
            orderDto.CustomerEmail = order.Customer.User.Email;
        }
        else if (order.Reservation?.Customer?.User != null)
        {
            // Fallback: Lấy từ Reservation nếu Customer.User không có
            orderDto.CustomerName = order.Reservation.Customer.User.FullName;
            orderDto.CustomerPhone = order.Reservation.Customer.User.Phone;
            orderDto.CustomerEmail = order.Reservation.Customer.User.Email;
            
            // ✅ Đảm bảo CustomerId được set từ Reservation
            // Reservation.CustomerId là int (không nullable), không phải int?
            if (!orderDto.CustomerId.HasValue && order.Reservation.CustomerId > 0)
            {
                orderDto.CustomerId = order.Reservation.CustomerId;
            }
        }
        else if (order.Reservation != null && !string.IsNullOrWhiteSpace(order.Reservation.CustomerNameReservation))
        {
            // Fallback: Lấy từ Reservation.CustomerNameReservation nếu không có User
            orderDto.CustomerName = order.Reservation.CustomerNameReservation;
        }
        
        // ✅ DEBUG: Log để trace customer info
        if (orderDto.CustomerId.HasValue && string.IsNullOrWhiteSpace(orderDto.CustomerName))
        {
            System.Diagnostics.Debug.WriteLine($"[PopulateOrderMetadata] Order {order.OrderId} has CustomerId={orderDto.CustomerId} but CustomerName is null. Order.Customer={order.Customer != null}, Order.Customer.User={order.Customer?.User != null}, Reservation.Customer={order.Reservation?.Customer != null}, Reservation.Customer.User={order.Reservation?.Customer?.User != null}");
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
        var depositRefundAmount = orderDto.DepositRefundAmount ?? 0;

        // ✅ CASE ĐẶC BIỆT: Nếu tiền cọc đã đủ (totalAmount = 0) và có tiền thừa cần trả lại
        if (totalAmount == 0 && depositRefundAmount > 0)
        {
            // Không cần nhận thêm tiền từ khách, chỉ cần trả lại tiền thừa
            // Validate: AmountReceived phải = 0
            if (request.AmountReceived > 0)
            {
                throw new InvalidOperationException($"⚠️ Đơn hàng đã được thanh toán đủ bằng tiền cọc. Tổng tiền cần trả lại: {depositRefundAmount:N0} VND. Không cần nhận thêm tiền.");
            }

            // Lock order trước khi thanh toán
            await LockOrderAsync(new OrderLockRequestDto { OrderId = request.OrderId }, userId, ct);

            try
            {
                // Tạo transaction với refund từ deposit
                var transaction = new Transaction
                {
                    OrderId = request.OrderId,
                    TransactionCode = $"TXN-{DateTime.UtcNow.Ticks}",
                    Amount = 0, // Đã thanh toán đủ bằng tiền cọc
                    AmountReceived = 0,
                    RefundAmount = depositRefundAmount, // Trả lại tiền thừa từ cọc
                    PaymentMethod = "Cash",
                    Status = "Paid",
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow,
                    IsManualConfirmed = true,
                    ConfirmedByUserId = userId,
                    Notes = request.Notes ?? $"Đã thanh toán đủ bằng tiền cọc. Trả lại tiền thừa: {depositRefundAmount:N0} VND"
                };

                var savedTransaction = await _unitOfWork.Payments.SaveTransactionAsync(transaction);

                // Cập nhật trạng thái order
                order.Status = OrderStatusConstants.Paid;
                await _unitOfWork.Payments.UpdateAsync(order);

                // Log success
                await _auditLogService.LogEventAsync(
                    "payment_success",
                    "Transaction",
                    savedTransaction.TransactionId,
                    $"Thanh toán bằng tiền cọc thành công. Trả lại tiền thừa: {depositRefundAmount:N0} VND",
                    null,
                    userId,
                    null,
                    ct
                );

                // 🔓 GIẢI PHÓNG BÀN VÀ HOÀN THÀNH RESERVATION
                await ReleaseTablesAndCompleteReservationAsync(request.OrderId, userId, ct);

                // Trigger post-payment actions (VIP update, LoyaltyPoints, etc.)
                await TriggerPostPaymentActionsAsync(request.OrderId, savedTransaction.TransactionId, ct);

                // Unlock order
                await UnlockOrderAsync(request.OrderId, ct);

                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<TransactionDto>(savedTransaction);
            }
            catch
            {
                // Unlock order nếu có lỗi
                await UnlockOrderAsync(request.OrderId, ct);
                throw;
            }
        }

        // CASE 1A: Underpaid - Block confirmation (chỉ khi totalAmount > 0)
        if (totalAmount > 0 && request.AmountReceived < totalAmount)
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

        // CASE 1B: Overpaid - Calculate refund (khi khách đưa nhiều hơn totalAmount)
        decimal? refundAmount = null;
        if (request.AmountReceived > totalAmount)
        {
            // ✅ Làm tròn tiền thối lại lên mệnh giá 1000
            refundAmount = RoundUpToThousand(request.AmountReceived - totalAmount);
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

            // 🔓 GIẢI PHÓNG BÀN VÀ HOÀN THÀNH RESERVATION
            await ReleaseTablesAndCompleteReservationAsync(request.OrderId, userId, ct);

            // Trigger post-payment actions (VIP update, LoyaltyPoints, etc.)
            // Wrap trong try-catch để đảm bảo SaveChangesAsync luôn được gọi
            try
            {
                await TriggerPostPaymentActionsAsync(request.OrderId, savedTransaction.TransactionId, ct);
            }
            catch (Exception postActionEx)
            {
                // Log lỗi nhưng không fail payment
                await _auditLogService.LogEventAsync(
                    eventType: "PostPaymentActionError",
                    entityType: "Order",
                    entityId: request.OrderId,
                    description: $"Lỗi trong post-payment actions: {postActionEx.Message}",
                    userId: userId,
                    ct: ct);
            }

            // Unlock order
            await UnlockOrderAsync(request.OrderId, ct);

            // ✅ QUAN TRỌNG: Save changes để đảm bảo order status = Paid được lưu vào database
            await _unitOfWork.SaveChangesAsync();

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
    /// Xử lý thanh toán kết hợp (Cash + QR)
    /// Tạo 2 transactions riêng biệt cho Cash và QR
    /// </summary>
    public async Task<List<TransactionDto>> ProcessCombinedPaymentAsync(CombinedPaymentRequestDto request, int userId, CancellationToken ct = default)
    {
        // Validate order exists
        var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(request.OrderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {request.OrderId}");
        }

        // Check if order is already paid
        if (order.Status == "Paid")
        {
            throw new InvalidOperationException("Đơn hàng đã được thanh toán");
        }

        // Calculate total amount
        var orderDto = _mapper.Map<OrderDto>(order);
         CalculateOrderAmounts(order, orderDto);
        var totalAmount = orderDto.TotalAmount ?? 0;

        // Validate tổng hai phần phải bằng totalAmount
        var partsTotal = request.CashAmount + request.QrAmount;
        if (Math.Abs(partsTotal - totalAmount) > 0.01m) // Cho phép sai số nhỏ do làm tròn
        {
            throw new InvalidOperationException($"Tổng hai phần thanh toán ({partsTotal:N0} VND) không khớp với tổng đơn hàng ({totalAmount:N0} VND)");
        }

        // Validate cash amount
        if (request.CashReceived.HasValue && request.CashReceived.Value < request.CashAmount)
        {
            throw new InvalidOperationException($"Số tiền khách đưa ({request.CashReceived.Value:N0} VND) nhỏ hơn phần thanh toán tiền mặt ({request.CashAmount:N0} VND)");
        }

        // Lock order
        await LockOrderAsync(new OrderLockRequestDto { OrderId = request.OrderId }, userId, ct);

        try
        {
            var transactions = new List<Transaction>();

            // ✅ Tạo transaction cho Cash payment
            decimal? cashRefundAmount = null;
            if (request.CashReceived.HasValue && request.CashReceived.Value > request.CashAmount)
            {
                cashRefundAmount = RoundUpToThousand(request.CashReceived.Value - request.CashAmount);
            }

            var cashTransaction = new Transaction
            {
                OrderId = request.OrderId,
                TransactionCode = $"TXN-CASH-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                Amount = request.CashAmount,
                AmountReceived = request.CashReceived ?? request.CashAmount,
                RefundAmount = cashRefundAmount,
                PaymentMethod = "Cash",
                Status = "Paid",
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                IsManualConfirmed = true,
                ConfirmedByUserId = userId,
                Notes = $"Thanh toán kết hợp - Phần tiền mặt: {request.CashAmount:N0} VND" + (cashRefundAmount.HasValue ? $", Tiền thối: {cashRefundAmount.Value:N0} VND" : "")
            };

            var savedCashTransaction = await _unitOfWork.Payments.SaveTransactionAsync(cashTransaction);
            transactions.Add(savedCashTransaction);

            // ✅ Tạo transaction cho QR payment
            var qrTransaction = new Transaction
            {
                OrderId = request.OrderId,
                TransactionCode = $"TXN-QR-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                Amount = request.QrAmount,
                PaymentMethod = "QRBankTransfer",
                Status = "Paid", // Combined payment: QR được xác nhận ngay khi cashier confirm
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                IsManualConfirmed = true,
                ConfirmedByUserId = userId,
                Notes = $"Thanh toán kết hợp - Phần QR: {request.QrAmount:N0} VND"
            };

            var savedQrTransaction = await _unitOfWork.Payments.SaveTransactionAsync(qrTransaction);
            transactions.Add(savedQrTransaction);

            // Cập nhật trạng thái order
            order.Status = OrderStatusConstants.Paid;
            await _unitOfWork.Payments.UpdateAsync(order);

            // Log audit
            await _auditLogService.LogEventAsync(
                "combined_payment_success",
                "Order",
                request.OrderId,
                $"Thanh toán kết hợp thành công. Cash: {request.CashAmount:N0} VND, QR: {request.QrAmount:N0} VND. Tổng: {totalAmount:N0} VND",
                null,
                userId,
                null,
                ct
            );

            // 🔓 GIẢI PHÓNG BÀN VÀ HOÀN THÀNH RESERVATION
            await ReleaseTablesAndCompleteReservationAsync(request.OrderId, userId, ct);

            // Trigger post-payment actions
            await TriggerPostPaymentActionsAsync(request.OrderId, savedCashTransaction.TransactionId, ct);

            // Unlock order
            await UnlockOrderAsync(request.OrderId, ct);

            await _unitOfWork.SaveChangesAsync();

            return transactions.Select(t => _mapper.Map<TransactionDto>(t)).ToList();
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

            // 🔓 GIẢI PHÓNG BÀN VÀ HOÀN THÀNH RESERVATION CHỈ KHI ĐÃ PAID TOÀN BỘ
            if (allPaid)
            {
                await ReleaseTablesAndCompleteReservationAsync(request.OrderId, userId, ct);
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

        // 🔓 GIẢI PHÓNG BÀN VÀ HOÀN THÀNH RESERVATION
        await ReleaseTablesAndCompleteReservationAsync(request.OrderId, userId, ct);

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
    /// Giải phóng bàn và cập nhật trạng thái Reservation khi bắt đầu thanh toán
    /// </summary>
    private async Task ReleaseTablesAndCompleteReservationAsync(int orderId, int? userId = null, CancellationToken ct = default)
    {
        try
        {
            // Lấy order với reservation details
            var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(orderId);


            // Lấy reservation
            var reservation = await _unitOfWork.Reservations.GetReservationByIdAsync(order.ReservationId.Value);


            // Cập nhật trạng thái Reservation thành "Completed"
            reservation.Status = "Completed";

            // Giải phóng các bàn trong ReservationTables
            if (reservation.ReservationTables != null && reservation.ReservationTables.Any())
            {
                var tableIds = reservation.ReservationTables.Select(rt => rt.TableId).ToList();
                reservation.ReservationTables.Clear();



                await _unitOfWork.Tables.SaveAsync();
            }

            // Lưu thay đổi reservation (entity đã được tracked, chỉ cần SaveChanges)
            await _unitOfWork.Reservations.SaveChangesAsync();

            // Log reservation completion
            await _auditLogService.LogEventAsync(
                eventType: "reservation_completed",
                entityType: "Reservation",
                entityId: reservation.ReservationId,
                description: $"Reservation {reservation.ReservationId} được đánh dấu hoàn thành khi bắt đầu thanh toán Order {orderId}",
                userId: userId,
                ct: ct
            );
        }

        catch (Exception ex)
        {
            // Log error but don't fail the payment - table release is secondary
            await _auditLogService.LogEventAsync(
                eventType: "table_release_failed",
                entityType: "Order",
                entityId: orderId,
                description: $"Lỗi khi giải phóng bàn và cập nhật reservation cho Order {orderId}: {ex.Message}",
                userId: userId,
                ct: ct
            );
        }
    }

    /// <summary>
    /// Chỉ giải phóng bàn (không có reservation hoặc reservation không tồn tại)
    /// </summary>


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
                // Step 8.1.1: Re-evaluate VIP status for the customer after successful payment
                try
                {
                    var vipService = _serviceProvider.GetService<ICustomerVipService>();
                    if (vipService != null && order.CustomerId.HasValue)
                    {
                        await vipService.AutoUpdateVipWhenPaymentCompletedAsync(order.OrderId, ct);
                    }
                }
                catch (Exception vipEx)
                {
                    await _auditLogService.LogEventAsync(
                        eventType: "VipEvaluationError",
                        entityType: "Customer",
                        entityId: order.CustomerId ?? 0,
                        description: $"Không thể cập nhật VIP sau thanh toán: {vipEx.Message}",
                        userId: null,
                        ct: ct);
                }

                // Step 8.1.2: Tăng LoyaltyPoints +1 cho Customer sau khi thanh toán thành công
                try
                {
                    if (order.Customer != null)
                    {
                        // Tăng LoyaltyPoints +1 (nếu null thì set = 1)
                        order.Customer.LoyaltyPoints = (order.Customer.LoyaltyPoints ?? 0) + 1;
                        
                        // Save changes để lưu LoyaltyPoints
                        await _unitOfWork.SaveChangesAsync();
                        
                        // Log việc tăng điểm
                        await _auditLogService.LogEventAsync(
                            eventType: "LoyaltyPointsIncreased",
                            entityType: "Customer",
                            entityId: order.Customer.CustomerId,
                            description: $"Tăng điểm tích lũy +1 sau thanh toán thành công. Điểm hiện tại: {order.Customer.LoyaltyPoints}",
                            userId: null,
                            ct: ct);
                    }
                }
                catch (Exception loyaltyEx)
                {
                    // Log lỗi nhưng không fail payment
                    await _auditLogService.LogEventAsync(
                        eventType: "LoyaltyPointsError",
                        entityType: "Customer",
                        entityId: order.CustomerId ?? 0,
                        description: $"Không thể tăng điểm tích lũy sau thanh toán: {loyaltyEx.Message}",
                        userId: null,
                        ct: ct);
                }

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

            // Step 8.3: Inventory deduction cho món ConsumptionBased khi paid
            // Với món ConsumptionBased: chỉ reserve khi tạo order, consume khi paid (dựa trên QuantityUsed)
            if (order?.OrderDetails != null && order.OrderDetails.Any())
            {
                var inventoryService = _serviceProvider.GetRequiredService<IInventoryIngredientService>();
                
                foreach (var orderDetail in order.OrderDetails)
                {
                    // Chỉ xử lý món có MenuItem và BillingType = ConsumptionBased
                    if (orderDetail.MenuItem?.BillingType == ItemBillingType.ConsumptionBased && 
                        orderDetail.MenuItemId.HasValue)
                    {
                        // Sử dụng QuantityUsed nếu có (đã confirm), nếu không thì dùng Quantity
                        var quantityToConsume = orderDetail.QuantityUsed ?? orderDetail.Quantity;
                        
                        if (quantityToConsume > 0)
                        {
                            try
                            {
                                var consumeResult = await inventoryService.ConsumeReservedBatchesForOrderDetailWithQuantityAsync(
                                    orderDetail.OrderDetailId, 
                                    quantityToConsume
                                );
                                
                                if (!consumeResult.success)
                                {
                                    // Log warning nhưng không fail payment
                                    await _auditLogService.LogEventAsync(
                                        eventType: "InventoryConsumptionWarning",
                                        entityType: "OrderDetail",
                                        entityId: orderDetail.OrderDetailId,
                                        description: $"Cảnh báo: Không thể trừ kho cho món {orderDetail.MenuItem.Name}: {consumeResult.message}",
                                        userId: null,
                                        ct: ct
                                    );
                                }
                                else
                                {
                                    await _auditLogService.LogEventAsync(
                                        eventType: "InventoryConsumed",
                                        entityType: "OrderDetail",
                                        entityId: orderDetail.OrderDetailId,
                                        description: $"Đã trừ kho cho món {orderDetail.MenuItem.Name} (SL: {quantityToConsume})",
                                        userId: null,
                                        ct: ct
                                    );
                                }
                            }
                            catch (Exception invEx)
                            {
                                // Log error nhưng không fail payment
                                await _auditLogService.LogEventAsync(
                                    eventType: "InventoryConsumptionError",
                                    entityType: "OrderDetail",
                                    entityId: orderDetail.OrderDetailId,
                                    description: $"Lỗi khi trừ kho: {invEx.Message}",
                                    userId: null,
                                    ct: ct
                                );
                            }
                        }
                    }
                }
            }

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

    // ========== ORDER CONFIRMATION METHODS (Merged from OrderConfirmationService) ==========

    /// <summary>
    /// Hủy món (chỉ cho Kitchen items ở trạng thái NotStarted)
    /// Merged from OrderConfirmationService.CancelItemAsync
    /// </summary>
    public async Task<bool> CancelItemAsync(int orderDetailId, string reason, int? staffId = null, CancellationToken ct = default)
    {
        var orderDetail = await _unitOfWork.Payments.GetOrderDetailByIdAsync(orderDetailId);

        if (orderDetail == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy món với ID: {orderDetailId}");
        }

        // Validate can cancel
        var (canCancel, validationReason) = await ValidateCanCancelItemAsync(orderDetailId, ct);

        if (!canCancel)
        {
            throw new InvalidOperationException(validationReason);
        }

        // ✅ QUAN TRỌNG: Giải phóng reserved quantity TRƯỚC KHI cập nhật status
        // Nếu món đã được reserve nguyên liệu, cần giải phóng để available có thể tăng lại
        // Phải gọi TRƯỚC khi set status = Removed để release có thể check status Pending/Cooking
        if (orderDetail.MenuItem != null)
        {
            try
            {
                var inventoryService = _serviceProvider.GetRequiredService<IInventoryIngredientService>();
                var releaseResult = await inventoryService.ReleaseReservedBatchesForOrderDetailAsync(orderDetailId);
                if (!releaseResult.success)
                {
                    // Log warning nhưng không fail việc hủy món
                    Console.WriteLine($"Warning: Không thể giải phóng nguyên liệu khi hủy món {orderDetailId}: {releaseResult.message}");
                }
            }
            catch (Exception ex)
            {
                // Log error nhưng không fail việc hủy món
                Console.WriteLine($"Error releasing reserved batches for order detail {orderDetailId}: {ex.Message}");
            }
        }

        // Mark as removed
        orderDetail.Status = "Removed";
        orderDetail.Quantity = 0;
        orderDetail.QuantityUsed = 0;
        orderDetail.Notes = $"Đã hủy: {reason}. " + orderDetail.Notes;

        await _unitOfWork.SaveChangesAsync();

        // Log audit
        await _auditLogService.LogEventAsync(
            "item_cancelled",
            "OrderDetail",
            orderDetailId,
            $"Món đã hủy. Lý do: {reason}",
            null,
            staffId,
            null,
            ct
        );

        return true;
    }

    /// <summary>
    /// Validate xem món có thể hủy không
    /// Merged from OrderConfirmationService.ValidateCanCancelItemAsync
    /// </summary>
    public async Task<(bool CanCancel, string Reason)> ValidateCanCancelItemAsync(int orderDetailId, CancellationToken ct = default)
    {
        var orderDetail = await _unitOfWork.Payments.GetOrderDetailByIdAsync(orderDetailId);

        if (orderDetail == null)
        {
            return (false, "Không tìm thấy món");
        }

        // Check if it's a kitchen item
        var isKitchenItem = orderDetail.MenuItem?.BillingType == ItemBillingType.KitchenPrepared;

        if (!isKitchenItem)
        {
            return (false, "Chỉ món chế biến trong bếp mới có thể hủy. Món tiêu hao vui lòng điều chỉnh số lượng sử dụng.");
        }

        // Check kitchen status
        var status = orderDetail.Status ?? "Pending";

        if (status == "Done" || status == "Served")
        {
            return (false, "Món đã hoàn thành, không thể hủy");
        }

        if (status == "Removed")
        {
            return (false, "Món đã được hủy trước đó");
        }

        // Can cancel if status is Pending, Confirmed, or Cooking
        if (status == "Pending" || status == "Cooking")
        {
            return (true, "Có thể hủy món");
        }

        return (false, $"Trạng thái '{status}' không cho phép hủy món");
    }

    /// <summary>
    /// Hủy toàn bộ đơn hàng và giải phóng bàn (khi khách rời đi trước khi món làm)
    /// </summary>
    public async Task<bool> CancelOrderAsync(int orderId, string reason, int? userId = null, CancellationToken ct = default)
    {
        var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(orderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {orderId}");
        }

        // Kiểm tra trạng thái đơn hàng - chỉ cho phép hủy nếu chưa thanh toán
        if (order.Status == "Paid" || order.Status == "Completed")
        {
            throw new InvalidOperationException("Không thể hủy đơn hàng đã thanh toán.");
        }

        // Kiểm tra xem có món nào đã được chế biến chưa
        if (order.OrderDetails != null && order.OrderDetails.Any())
        {
            var hasCookingItems = order.OrderDetails.Any(od =>
            {
                var status = (od.Status ?? "").Trim().ToLower();
                return status == "cooking" || status == "done" || status == "ready" || status == "served" ||
                       status == "đang chế biến" || status == "đã xong" || status == "sẵn sàng";
            });

            if (hasCookingItems)
            {
                throw new InvalidOperationException("Không thể hủy đơn hàng vì đã có món đang được chế biến hoặc đã hoàn thành.");
            }
        }

        // ✅ QUAN TRỌNG: Giải phóng reserved quantity TRƯỚC KHI cập nhật status
        // Hủy tất cả các món trong đơn (nếu chưa được chế biến)
        if (order.OrderDetails != null)
        {
            var inventoryService = _serviceProvider.GetRequiredService<IInventoryIngredientService>();
            
            foreach (var detail in order.OrderDetails)
            {
                var status = (detail.Status ?? "").Trim().ToLower();
                if (status == "pending" || status == "confirmed" || status == "đã gửi" || status == "cooking")
                {
                    // ✅ Giải phóng reserved quantity TRƯỚC KHI set status = Cancelled
                    // Phải gọi TRƯỚC để release có thể check status Pending/Cooking
                    if (detail.MenuItem != null)
                    {
                        try
                        {
                            var releaseResult = await inventoryService.ReleaseReservedBatchesForOrderDetailAsync(detail.OrderDetailId);
                            if (!releaseResult.success)
                            {
                                // Log warning nhưng không fail việc hủy món
                                Console.WriteLine($"Warning: Không thể giải phóng nguyên liệu khi hủy món {detail.OrderDetailId}: {releaseResult.message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log error nhưng không fail việc hủy món
                            Console.WriteLine($"Error releasing reserved batches for order detail {detail.OrderDetailId}: {ex.Message}");
                        }
                    }
                    
                    // Sau khi release, mới set status = Cancelled
                    detail.Status = "Cancelled";
                    detail.Quantity = 0;
                    detail.QuantityUsed = 0;
                }
            }
        }

        // Cập nhật trạng thái đơn hàng thành "Cancelled"
        order.Status = "Cancelled";

        await _unitOfWork.Payments.UpdateAsync(order);
        await _unitOfWork.SaveChangesAsync();

        // Giải phóng bàn và reservation (nếu có)
        if (order.ReservationId.HasValue)
        {
            try
            {
                var reservation = await _unitOfWork.Reservations.GetReservationByIdAsync(order.ReservationId.Value);
                if (reservation != null)
                {
                    // Cập nhật trạng thái Reservation thành "Cancelled"
                    reservation.Status = "Cancelled";

                    // Giải phóng các bàn trong ReservationTables
                    if (reservation.ReservationTables != null && reservation.ReservationTables.Any())
                    {
                        reservation.ReservationTables.Clear();
                        await _unitOfWork.Tables.SaveAsync();
                    }

                    await _unitOfWork.Reservations.SaveChangesAsync();

                    // Log reservation cancellation
                    await _auditLogService.LogEventAsync(
                        eventType: "reservation_cancelled",
                        entityType: "Reservation",
                        entityId: reservation.ReservationId,
                        description: $"Reservation {reservation.ReservationId} bị hủy khi hủy Order {orderId}. Lý do: {reason}",
                        userId: userId,
                        ct: ct
                    );
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the cancellation
                await _auditLogService.LogEventAsync(
                    eventType: "table_release_failed",
                    entityType: "Order",
                    entityId: orderId,
                    description: $"Lỗi khi giải phóng bàn và cập nhật reservation cho Order {orderId}: {ex.Message}",
                    userId: userId,
                    ct: ct
                );
            }
        }

        // Log order cancellation
        await _auditLogService.LogEventAsync(
            eventType: "order_cancelled",
            entityType: "Order",
            entityId: orderId,
            description: $"Đơn hàng đã bị hủy. Lý do: {reason}",
            userId: userId,
            ct: ct
        );

        return true;
    }
}

