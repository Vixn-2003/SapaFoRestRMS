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
using Microsoft.AspNetCore.Authorization;

namespace BusinessAccessLayer.Services;

/// <summary>
/// Service xử lý business logic cho Payment
/// </summary>
/// 
[Authorize(Policy = "Position:Cashier")]

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
        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Now);

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

        //  Filter theo ngày: ưu tiên PaidAt, fallback CreatedAt cho đơn chưa thanh toán
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
            //  Hỗ trợ cả "pending"/"processed" và "Confirmed"/"Paid"
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
                //  Filter theo status chính xác nếu không match với pending/processed
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
        //  DEBUG: Log để trace
        System.Diagnostics.Debug.WriteLine($"[GetOrderDetailAsync] Loading order {orderId}");
        
        var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(orderId);

        if (order == null)
        {
            System.Diagnostics.Debug.WriteLine($"[GetOrderDetailAsync] Order {orderId} not found");
            return null;
        }

        //  DEBUG: Log order data trước khi map
        System.Diagnostics.Debug.WriteLine($"[GetOrderDetailAsync] Order {orderId} - CustomerId: {order.CustomerId}, ReservationId: {order.ReservationId}, Customer: {order.Customer != null}, Customer.User: {order.Customer?.User != null}, Reservation.Customer: {order.Reservation?.Customer != null}, Reservation.Customer.User: {order.Reservation?.Customer?.User != null}");

        var orderDto = _mapper.Map<OrderDto>(order);

        // Tính toán các khoản tiền
        CalculateOrderAmounts(order, orderDto);
        PopulateOrderMetadata(order, orderDto);
        
        //  DEBUG: Log orderDto sau khi populate
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
            
            //  VALIDATION ĐẦY ĐỦ: Code, IsDelete, Status, và THỜI HẠN
            var voucher = vouchers.FirstOrDefault(v =>
                string.Equals(v.Code, request.VoucherCode!.Trim(), StringComparison.OrdinalIgnoreCase) &&
                v.IsDelete != true &&
                string.Equals(v.Status, "Đang sử dụng", StringComparison.OrdinalIgnoreCase) &&
                //  Check thời hạn: StartDate <= today <= EndDate
                (!v.StartDate.HasValue || v.StartDate.Value.Date <= today) &&
                (!v.EndDate.HasValue || v.EndDate.Value.Date >= today));

            if (voucher == null)
            {
                throw new KeyNotFoundException("Mã giảm giá không hợp lệ, đã hết hạn hoặc chưa đến thời gian sử dụng.");
            }

            var subtotal = orderDto.Subtotal ?? 0;

            //  Check điều kiện giá trị tối thiểu
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

        // ✅ FIX: KHÔNG làm tròn discount amount - giữ nguyên giá trị tính toán
        orderDto.DiscountAmount = discountAmount;

        // Tính lại tổng tiền sau ưu đãi (KHÔNG làm tròn các thành phần trung gian)
        var totalBeforeRounding = (orderDto.Subtotal ?? 0) + (orderDto.VatAmount ?? 0) +
                                  (orderDto.ServiceFee ?? 0) - orderDto.DiscountAmount.Value;
        
        // ✅ CHỈ làm tròn số tiền cuối cùng khách phải trả (TotalAmount)
        orderDto.TotalAmount = RoundUpToThousand(totalBeforeRounding);

        //  FIX: Lưu discount vào Payment record trong database
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
                DiscountAmount = discountAmount,
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
            payment.DiscountAmount = discountAmount;
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

    /// <summary>
    /// Áp dụng ưu đãi/giảm giá cho Reservation (áp dụng cho tất cả Orders trong Reservation)
    /// </summary>
    public async Task<ReservationPaymentDto> ApplyDiscountByReservationAsync(ReservationDiscountRequestDto request, CancellationToken ct = default)
    {
        // Lấy tất cả Orders trong Reservation
        var orders = await _unitOfWork.Payments.GetOrdersByReservationIdAsync(request.ReservationId);
        var ordersList = orders.ToList();

        if (!ordersList.Any())
        {
            throw new KeyNotFoundException($"Không tìm thấy Orders nào cho Reservation với ID: {request.ReservationId}");
        }

        // Lấy thông tin Reservation payment để tính tổng Subtotal
        var reservationPayment = await GetReservationPaymentAsync(request.ReservationId, ct);
        if (reservationPayment == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Reservation với ID: {request.ReservationId}");
        }

        decimal discountAmount = request.DiscountAmount ?? 0;

        // Nếu có VoucherCode → áp dụng logic Voucher dựa trên tổng Subtotal của Reservation
        if (!string.IsNullOrWhiteSpace(request.VoucherCode))
        {
            var voucherService = _serviceProvider.GetService<IVoucherService>();
            if (voucherService == null)
            {
                throw new Exception("VoucherService chưa được cấu hình trong hệ thống.");
            }

            var vouchers = await voucherService.GetAllAsync();
            var today = DateTime.Today;
            
            // VALIDATION ĐẦY ĐỦ: Code, IsDelete, Status, và THỜI HẠN
            var voucher = vouchers.FirstOrDefault(v =>
                string.Equals(v.Code, request.VoucherCode!.Trim(), StringComparison.OrdinalIgnoreCase) &&
                v.IsDelete != true &&
                string.Equals(v.Status, "Đang sử dụng", StringComparison.OrdinalIgnoreCase) &&
                (!v.StartDate.HasValue || v.StartDate.Value.Date <= today) &&
                (!v.EndDate.HasValue || v.EndDate.Value.Date >= today));

            if (voucher == null)
            {
                throw new KeyNotFoundException("Mã giảm giá không hợp lệ, đã hết hạn hoặc chưa đến thời gian sử dụng.");
            }

            // ✅ Sử dụng tổng Subtotal của Reservation (tổng hợp tất cả Orders)
            var reservationSubtotal = reservationPayment.Subtotal;

            // Check điều kiện giá trị tối thiểu dựa trên tổng Subtotal của Reservation
            if (voucher.MinOrderValue.HasValue && reservationSubtotal < voucher.MinOrderValue.Value)
            {
                throw new InvalidOperationException(
                    $"Tổng giá trị đơn hàng chưa đủ giá trị tối thiểu {voucher.MinOrderValue.Value:N0} ₫ để áp dụng voucher này.");
            }

            // Tính mức giảm theo loại voucher (dựa trên tổng Subtotal của Reservation)
            if (string.Equals(voucher.DiscountType, "Phần trăm", StringComparison.OrdinalIgnoreCase))
            {
                var raw = reservationSubtotal * (voucher.DiscountValue / 100m);
                discountAmount = voucher.MaxDiscount.HasValue
                    ? Math.Min(raw, voucher.MaxDiscount.Value)
                    : raw;
            }
            else // "Giá trị cố định"
            {
                discountAmount = voucher.DiscountValue;
            }

            // Không cho giảm quá tổng Subtotal của Reservation
            if (discountAmount > reservationSubtotal)
            {
                discountAmount = reservationSubtotal;
            }
        }

        // ✅ FIX: KHÔNG làm tròn discount amount - giữ nguyên giá trị tính toán
        // ✅ Phân bổ discount cho tất cả Orders trong Reservation theo tỷ lệ Subtotal
        // Ví dụ: Order1 có Subtotal = 100k, Order2 có Subtotal = 200k, Total = 300k
        // Discount = 30k → Order1: 10k, Order2: 20k
        
        // Tính tổng Subtotal của tất cả Orders (sử dụng reservationPayment.Subtotal đã tính sẵn)
        decimal totalSubtotal = reservationPayment.Subtotal;

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

        // Phân bổ discount cho từng Order
        foreach (var order in ordersList)
        {
            var orderDto = _mapper.Map<OrderDto>(order);
            CalculateOrderAmounts(order, orderDto);
            
            // Tính discount cho Order này theo tỷ lệ Subtotal
            decimal orderDiscount = 0;
            if (totalSubtotal > 0 && orderDto.Subtotal.HasValue)
            {
                var ratio = orderDto.Subtotal.Value / totalSubtotal;
                orderDiscount = discountAmount * ratio;
            }
            else if (ordersList.Count == 1)
            {
                // Nếu chỉ có 1 Order, áp dụng toàn bộ discount
                orderDiscount = discountAmount;
            }

            // ✅ FIX: KHÔNG làm tròn discount - giữ nguyên giá trị tính toán
            orderDto.DiscountAmount = orderDiscount;

            // Tính lại tổng tiền sau ưu đãi cho Order này (KHÔNG làm tròn các thành phần trung gian)
            var orderTotalBeforeRounding = (orderDto.Subtotal ?? 0) + (orderDto.VatAmount ?? 0) +
                                          (orderDto.ServiceFee ?? 0) - orderDiscount;
            
            // ✅ CHỈ làm tròn số tiền cuối cùng khách phải trả (TotalAmount)
            orderDto.TotalAmount = RoundUpToThousand(orderTotalBeforeRounding);

            // Lưu discount vào Payment record của Order
            var payment = order.Payments?.OrderByDescending(p => p.PaymentDate ?? DateTime.MinValue).FirstOrDefault();
            
            if (payment == null)
            {
                payment = new Payment
                {
                    OrderId = order.OrderId,
                    PaymentMethod = "Pending",
                    Subtotal = orderDto.Subtotal ?? 0,
                    DiscountAmount = orderDiscount,
                    Vatpercent = 10,
                    Vatamount = orderDto.VatAmount ?? 0,
                    FinalAmount = orderDto.TotalAmount ?? 0,
                    VoucherId = voucherId,
                    PaymentDate = null
                };
                if (order.Payments == null)
                {
                    order.Payments = new List<Payment>();
                }
                order.Payments.Add(payment);
            }
            else
            {
                // Cập nhật Payment record hiện có
                payment.DiscountAmount = orderDiscount;
                payment.VoucherId = voucherId;
                payment.Subtotal = orderDto.Subtotal ?? 0;
                payment.Vatamount = orderDto.VatAmount ?? 0;
                payment.FinalAmount = orderDto.TotalAmount ?? 0;
            }
        }

        // Update tất cả Orders để trigger save Payment changes
        foreach (var order in ordersList)
        {
            await _unitOfWork.Payments.UpdateAsync(order);
        }
        
        // Save changes để lưu discount vào database
        await _unitOfWork.SaveChangesAsync();

        // Trả về ReservationPaymentDto đã được cập nhật với discount
        return await GetReservationPaymentAsync(request.ReservationId, ct) 
            ?? throw new InvalidOperationException($"Không thể lấy thông tin Reservation sau khi áp dụng discount.");
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
        var sessionId = $"SESSION-{DateTime.Now.Ticks}-{request.OrderId}";

        // Tạo transaction record
        var transaction = new Transaction
        {
            OrderId = request.OrderId,
            TransactionCode = $"TXN-{DateTime.Now.Ticks}",
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            Status = "Pending",
            CreatedAt = DateTime.Now,
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

        //  FIX: Nếu có SessionId, tìm transaction đã tạo từ InitiatePayment và sử dụng amount từ đó
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
            // ✅ FIX: Ưu tiên sử dụng Order.TotalAmount đã lưu trong database (nếu có)
            // Điều này đảm bảo consistency với API response
            if (order.TotalAmount.HasValue && order.TotalAmount.Value > 0)
            {
                expectedAmount = order.TotalAmount.Value;
              
            }
            else
            {
                // Nếu Order.TotalAmount chưa được set, tính lại từ order details
                var orderDto = _mapper.Map<OrderDto>(order);
                
                // ✅ DEBUG: Log reservation info để debug deposit calculation
                if (order.Reservation != null)
                {
                    
                }
                
                CalculateOrderAmounts(order, orderDto);
                expectedAmount = orderDto.TotalAmount ?? 0;
              
            }

            // ✅ FIX: Log warning nếu request amount khác, nhưng vẫn sử dụng expectedAmount từ database
            if (Math.Abs(request.Amount - expectedAmount) > 0.01m)
            {
               
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
            //  Cập nhật transaction đã tồn tại thay vì tạo mới
            existingTransaction.Status = "Paid";
            existingTransaction.CompletedAt = DateTime.Now;
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
            // ✅ FIX: Sử dụng expectedAmount (từ database) thay vì request.Amount
            var transaction = new Transaction
            {
                OrderId = request.OrderId,
                TransactionCode = $"TXN-{DateTime.Now.Ticks}",
                Amount = expectedAmount, // Sử dụng expectedAmount từ database
                PaymentMethod = request.PaymentMethod,
                Status = "Paid",
                CreatedAt = DateTime.Now,
                CompletedAt = DateTime.Now,
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

        //  Trigger post-payment actions (VIP update, LoyaltyPoints +1, etc.)
        await TriggerPostPaymentActionsAsync(request.OrderId, savedTransaction.TransactionId, ct);

        return _mapper.Map<TransactionDto>(savedTransaction);
    }

    public async Task<OrderDto> ConfirmOrderAsync(CustomerConfirmRequestDto request, int userId, CancellationToken ct = default)
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

        //  Danh sách status được phép thanh toán (billable)
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
                //  QUAN TRỌNG: Giải phóng reserved quantity TRƯỚC KHI cập nhật status
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
                //  FIX BUG: KHÔNG ghi đè Quantity (SL đặt)
                // Chỉ cập nhật QuantityUsed (SL thực tế khách dùng)
                // Giữ nguyên detail.Quantity (đây là SL ban đầu đặt)
                detail.QuantityUsed = confirmed.QuantityUsed < 0 ? 0 : confirmed.QuantityUsed;
                
                //  FIX BUG: CHỈ chuyển status thành "Done" nếu món có status billable
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

        //  TỰ ĐỘNG CHUYỂN TRẠNG THÁI CÁC MÓN CÓ STATUS "Cooking", "Done", "Ready", "Served" THÀNH "Done"
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

            //  Bỏ qua món đã được xử lý trong request.Items (đã được set status ở trên)
            // Kiểm tra xem món này có trong request.Items không
            var wasProcessedInRequest = request.Items.Any(item => item.OrderDetailId == detail.OrderDetailId);
            if (wasProcessedInRequest)
            {
                continue; // Đã xử lý rồi, không xử lý lại
            }

            var currentStatus = (detail.Status ?? "").Trim();
            
            //  XỬ LÝ MÓN LẺ (KHÔNG PHẢI COMBO)
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
            //  XỬ LÝ COMBO
            else if (detail.ComboId.HasValue)
            {
                // ⚠️ CHỈ chuyển status thành "Done" nếu combo có status billable
                // KHÔNG chuyển combo "Pending" thành "Done"
                if (billableStatusesForAutoUpdate.Any(s => string.Equals(s, currentStatus, StringComparison.OrdinalIgnoreCase)))
                {
                    detail.Status = "Done";
                    
                    //  CHUYỂN TRẠNG THÁI TẤT CẢ MÓN CON TRONG COMBO THÀNH "Done" (chỉ món con có status billable)
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

        // Tính toán tổng tiền trước khi lưu
        var orderDto = _mapper.Map<OrderDto>(order);
        CalculateOrderAmounts(order, orderDto);
        PopulateOrderMetadata(order, orderDto);

        // Sau khi khách xác nhận, chuyển trạng thái đơn sang "Confirmed" (đã xác nhận, chờ thanh toán)
        order.Status = OrderStatusConstants.Confirmed;
        order.ConfirmedAt = DateTime.Now;

        // Lưu staffId của người thực hiện xác nhận
        var staffId = await ResolveStaffIdAsync(userId, ct);
        order.ConfirmedByStaffId = staffId;

        // Lưu TotalAmount sau khi đã tính toán
        order.TotalAmount = orderDto.TotalAmount;

        await _unitOfWork.SaveChangesAsync();

        // Ghi lại lịch sử xác nhận đơn hàng
        var history = new OrderHistory
        {
            OrderId = request.OrderId,
            Action = "Order Confirmation",
            Reason = $"Confirmed by staff. Total amount: {orderDto.TotalAmount:N0} VND",
            StaffId = staffId,
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.Payments.AddOrderHistoryAsync(history);
        await _unitOfWork.SaveChangesAsync();

        return orderDto;
    }

    /// <summary>
    /// Xác nhận Reservation (confirm tất cả Orders trong Reservation)
    /// </summary>
    public async Task<ReservationPaymentDto> ConfirmReservationAsync(ReservationConfirmRequestDto request, int userId, CancellationToken ct = default)
    {
        // Lấy tất cả Orders trong Reservation
        var orders = await _unitOfWork.Payments.GetOrdersByReservationIdAsync(request.ReservationId);
        var ordersList = orders.ToList();

        if (!ordersList.Any())
        {
            throw new KeyNotFoundException($"Không tìm thấy Orders nào cho Reservation với ID: {request.ReservationId}");
        }

        // Validate: Tất cả Orders phải có OrderDetails
        var ordersWithoutItems = ordersList.Where(o => o.OrderDetails == null || !o.OrderDetails.Any()).ToList();
        if (ordersWithoutItems.Any())
        {
            throw new InvalidOperationException($"Có {ordersWithoutItems.Count} đơn hàng không có món để xác nhận.");
        }

        // Danh sách status được phép thanh toán (billable)
        var billableStatuses = new[] { "Cooking", "Done", "Ready", "Served", "cooking", "done", "ready", "served", "Đang chế biến", "Đã xong", "Sẵn sàng", "Đã phục vụ" };
        var billableStatusesForAutoUpdate = new[] { "Cooking", "Done", "Ready", "Served", "cooking", "done", "ready", "served", "Đang chế biến", "Đã xong", "Sẵn sàng", "Đã phục vụ" };

        var staffId = await ResolveStaffIdAsync(userId, ct);

        // Xử lý từng Order trong Reservation
        foreach (var order in ordersList)
        {
            // Lấy danh sách items cần confirm cho Order này (nếu có trong request)
            var orderItemsToConfirm = request.OrderItems.ContainsKey(order.OrderId)
                ? request.OrderItems[order.OrderId]
                : new List<CustomerConfirmedItemDto>();

            // Xử lý các items được chỉ định trong request
            foreach (var confirmed in orderItemsToConfirm)
            {
                var detail = order.OrderDetails.FirstOrDefault(d => d.OrderDetailId == confirmed.OrderDetailId);
                if (detail == null)
                {
                    continue;
                }

                if (confirmed.IsRemoved)
                {
                    // QUAN TRỌNG: Giải phóng reserved quantity TRƯỚC KHI cập nhật status
                    if (detail.MenuItem != null)
                    {
                        try
                        {
                            var inventoryService = _serviceProvider.GetRequiredService<IInventoryIngredientService>();
                            var releaseResult = await inventoryService.ReleaseReservedBatchesForOrderDetailAsync(detail.OrderDetailId);
                            if (!releaseResult.success)
                            {
                                Console.WriteLine($"Warning: Không thể giải phóng nguyên liệu khi hủy món {detail.OrderDetailId}: {releaseResult.message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error releasing reserved batches for order detail {detail.OrderDetailId}: {ex.Message}");
                        }
                    }

                    detail.Quantity = 0;
                    detail.QuantityUsed = 0;
                    detail.Status = "Removed";
                }
                else
                {
                    detail.QuantityUsed = confirmed.QuantityUsed < 0 ? 0 : confirmed.QuantityUsed;

                    var currentStatus = (detail.Status ?? "").Trim();
                    bool isBillable = billableStatuses.Any(s => string.Equals(s, currentStatus, StringComparison.OrdinalIgnoreCase));

                    if (isBillable)
                    {
                        detail.Status = "Done";
                    }
                }
            }

            // TỰ ĐỘNG CHUYỂN TRẠNG THÁI CÁC MÓN CÓ STATUS BILLABLE THÀNH "Done"
            foreach (var detail in order.OrderDetails)
            {
                // Bỏ qua món đã bị hủy
                if (detail.Status == "Removed" || detail.Status == "Cancelled")
                {
                    continue;
                }

                // Bỏ qua món đã được xử lý trong request
                var wasProcessedInRequest = orderItemsToConfirm.Any(item => item.OrderDetailId == detail.OrderDetailId);
                if (wasProcessedInRequest)
                {
                    continue;
                }

                var currentStatus = (detail.Status ?? "").Trim();

                // XỬ LÝ MÓN LẺ (KHÔNG PHẢI COMBO)
                if (!detail.ComboId.HasValue)
                {
                    if (billableStatusesForAutoUpdate.Any(s => string.Equals(s, currentStatus, StringComparison.OrdinalIgnoreCase)))
                    {
                        detail.Status = "Done";

                        if (detail.MenuItem?.BillingType == ItemBillingType.ConsumptionBased &&
                            !detail.QuantityUsed.HasValue)
                        {
                            detail.QuantityUsed = detail.Quantity;
                        }
                    }
                }
                // XỬ LÝ COMBO
                else if (detail.ComboId.HasValue)
                {
                    if (billableStatusesForAutoUpdate.Any(s => string.Equals(s, currentStatus, StringComparison.OrdinalIgnoreCase)))
                    {
                        detail.Status = "Done";

                        if (detail.OrderComboItems != null && detail.OrderComboItems.Any())
                        {
                            foreach (var comboItem in detail.OrderComboItems)
                            {
                                var comboItemStatus = (comboItem.Status ?? "").Trim();
                                if (billableStatusesForAutoUpdate.Any(s => string.Equals(s, comboItemStatus, StringComparison.OrdinalIgnoreCase)))
                                {
                                    comboItem.Status = "Done";
                                }
                            }
                        }
                    }
                }
            }

            // Tính toán tổng tiền cho Order
            var orderDto = _mapper.Map<OrderDto>(order);
            CalculateOrderAmounts(order, orderDto);
            PopulateOrderMetadata(order, orderDto);

            // Chuyển trạng thái Order sang "Confirmed"
            order.Status = OrderStatusConstants.Confirmed;
            order.ConfirmedAt = DateTime.Now;
            order.ConfirmedByStaffId = staffId;
            order.TotalAmount = orderDto.TotalAmount;

            // Ghi lại lịch sử xác nhận đơn hàng
            var history = new OrderHistory
            {
                OrderId = order.OrderId,
                Action = "Order Confirmation (Reservation)",
                Reason = $"Confirmed by staff for Reservation {request.ReservationId}. Total amount: {orderDto.TotalAmount:N0} VND",
                StaffId = staffId,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.Payments.AddOrderHistoryAsync(history);
        }

        // Lưu tất cả thay đổi
        await _unitOfWork.SaveChangesAsync();

        // Trả về ReservationPaymentDto với tất cả Orders đã được confirm
        var reservationPayment = await GetReservationPaymentAsync(request.ReservationId, ct);
        if (reservationPayment == null)
        {
            throw new InvalidOperationException($"Không thể lấy thông tin thanh toán cho Reservation {request.ReservationId} sau khi confirm.");
        }

        return reservationPayment;
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
            CreatedAt = DateTime.Now
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
    /// Làm tròn XUỐNG mệnh giá 1000 VND (dùng cho discount để tránh thiệt hại cho khách)
    /// Ví dụ: 112500 → 112000, 112400 → 112000, 112000 → 112000
    /// </summary>
    private static decimal RoundDownToThousand(decimal amount)
    {
        if (amount <= 0) return 0;
        return Math.Floor(amount / 1000m) * 1000m;
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

                //  LOGIC: Phân biệt 2 loại món
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

        // ✅ FIX: KHÔNG làm tròn Subtotal - giữ nguyên giá trị tính toán
        orderDto.Subtotal = subtotal;

        // ✅ FIX: Tính VAT (10%) từ Subtotal (KHÔNG làm tròn)
        orderDto.VatAmount = orderDto.Subtotal.Value * 0.1m;

        // ✅ FIX: Tính phí dịch vụ (5%) từ Subtotal (KHÔNG làm tròn)
        orderDto.ServiceFee = orderDto.Subtotal.Value * 0.05m;

        // Lấy discount từ Payment nếu có (không cần làm tròn lại vì đã làm tròn khi lưu)
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

        //  XỬ LÝ TRƯỜNG HỢP DEPOSIT > TOTAL
        // Nếu tiền cọc lớn hơn tổng tiền thanh toán, cần trả lại tiền thừa cho khách
        if (depositToDeduct > 0 && depositToDeduct > totalBeforeDeposit)
        {
            // Tính số tiền cần trả lại (KHÔNG làm tròn - giữ nguyên)
            orderDto.DepositRefundAmount = depositToDeduct - totalBeforeDeposit;
            // Tổng tiền thanh toán = 0 (vì đã đủ tiền cọc)
            orderDto.TotalAmount = 0;
        }
        else
        {
            // ✅ CHỈ làm tròn số tiền cuối cùng khách phải trả (TotalAmount)
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

        //  FIX: Đảm bảo CustomerId được set (từ Order hoặc Reservation)
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
        
        //  FIX: Lấy customer info từ Customer.User hoặc Reservation.Customer.User
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
            
            //  Đảm bảo CustomerId được set từ Reservation
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
        
        //  DEBUG: Log để trace customer info
        if (orderDto.CustomerId.HasValue && string.IsNullOrWhiteSpace(orderDto.CustomerName))
        {
            System.Diagnostics.Debug.WriteLine($"[PopulateOrderMetadata] Order {order.OrderId} has CustomerId={orderDto.CustomerId} but CustomerName is null. Order.Customer={order.Customer != null}, Order.Customer.User={order.Customer?.User != null}, Reservation.Customer={order.Reservation?.Customer != null}, Reservation.Customer.User={order.Reservation?.Customer?.User != null}");
        }

        // ✅ FIX: WaiterName (Nhân viên phục vụ) lấy từ Order.ConfirmedByStaff (waiter xác nhận order)
        if (order.ConfirmedByStaff != null && order.ConfirmedByStaff.User != null)
        {
            orderDto.WaiterName = order.ConfirmedByStaff.User.FullName;
        }

        // ✅ FIX: StaffName (Thu ngân xử lý) lấy từ Transaction.ConfirmedByUser (staff xử lý thanh toán)
        if (order.Transactions != null && order.Transactions.Any())
        {
            var paidTransactions = order.Transactions
                .Where(t =>
                    string.Equals(t.Status, "Success", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t.Status, "Paid", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (paidTransactions.Any())
            {
                var latestPaidTransaction = paidTransactions
                    .OrderByDescending(t => t.CompletedAt ?? t.CreatedAt)
                    .FirstOrDefault();

                if (latestPaidTransaction != null)
                {
                    orderDto.PaidAt = latestPaidTransaction.CompletedAt ?? latestPaidTransaction.CreatedAt;
                    
                    // ✅ FIX: Lấy StaffName từ Transaction.ConfirmedByUser (staff xử lý thanh toán)
                    if (latestPaidTransaction.ConfirmedByUser != null)
                    {
                        orderDto.StaffName = latestPaidTransaction.ConfirmedByUser.FullName;
                    }
                    
                    // ✅ FIX: Kiểm tra nếu có combined payment (cả Cash và QRBankTransfer)
                    var distinctPaymentMethods = paidTransactions
                        .Select(t => t.PaymentMethod)
                        .Where(pm => !string.IsNullOrWhiteSpace(pm))
                        .Distinct()
                        .ToList();

                    if (distinctPaymentMethods.Count > 1 && 
                        distinctPaymentMethods.Contains("Cash", StringComparer.OrdinalIgnoreCase) &&
                        (distinctPaymentMethods.Any(pm => pm.Contains("QR", StringComparison.OrdinalIgnoreCase)) ||
                         distinctPaymentMethods.Any(pm => pm.Contains("BankTransfer", StringComparison.OrdinalIgnoreCase))))
                    {
                        // Combined payment: Cash + QR
                        orderDto.PaymentMethod = "Combined";
                    }
                    else
                    {
                        // Single payment method
                        orderDto.PaymentMethod = latestPaidTransaction.PaymentMethod;
                    }
                }
            }
        }

        // Fallback: Nếu không có StaffName từ transaction, thử lấy từ Reservation.Staff (legacy)
        if (string.IsNullOrWhiteSpace(orderDto.StaffName) && order.Reservation?.Staff != null)
        {
            orderDto.StaffName = order.Reservation.Staff.FullName;
        }

        // Fallback: Nếu không có WaiterName từ ConfirmedByStaff, thử lấy từ Reservation.Staff (legacy)
        if (string.IsNullOrWhiteSpace(orderDto.WaiterName) && order.Reservation?.Staff != null)
        {
            orderDto.WaiterName = order.Reservation.Staff.FullName;
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

        //  CASE ĐẶC BIỆT: Nếu tiền cọc đã đủ (totalAmount = 0) và có tiền thừa cần trả lại
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
                    TransactionCode = $"TXN-{DateTime.Now.Ticks}",
                    Amount = 0, // Đã thanh toán đủ bằng tiền cọc
                    AmountReceived = 0,
                    RefundAmount = depositRefundAmount, // Trả lại tiền thừa từ cọc
                    PaymentMethod = "Cash",
                    Status = "Paid",
                    CreatedAt = DateTime.Now,
                    CompletedAt = DateTime.Now,
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
            //  Làm tròn tiền thối lại lên mệnh giá 1000
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
                TransactionCode = $"TXN-{DateTime.Now.Ticks}",
                Amount = totalAmount,
                AmountReceived = request.AmountReceived,
                RefundAmount = refundAmount,
                PaymentMethod = "Cash",
                Status = "Paid",
                CreatedAt = DateTime.Now,
                CompletedAt = DateTime.Now,
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

            //  QUAN TRỌNG: Save changes để đảm bảo order status = Paid được lưu vào database
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

            //  Tạo transaction cho Cash payment
            decimal? cashRefundAmount = null;
            if (request.CashReceived.HasValue && request.CashReceived.Value > request.CashAmount)
            {
                cashRefundAmount = RoundUpToThousand(request.CashReceived.Value - request.CashAmount);
            }

            var cashTransaction = new Transaction
            {
                OrderId = request.OrderId,
                TransactionCode = $"TXN-CASH-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                Amount = request.CashAmount,
                AmountReceived = request.CashReceived ?? request.CashAmount,
                RefundAmount = cashRefundAmount,
                PaymentMethod = "Cash",
                Status = "Paid",
                CreatedAt = DateTime.Now,
                CompletedAt = DateTime.Now,
                IsManualConfirmed = true,
                ConfirmedByUserId = userId,
                Notes = $"Thanh toán kết hợp - Phần tiền mặt: {request.CashAmount:N0} VND" + (cashRefundAmount.HasValue ? $", Tiền thối: {cashRefundAmount.Value:N0} VND" : "")
            };

            var savedCashTransaction = await _unitOfWork.Payments.SaveTransactionAsync(cashTransaction);
            transactions.Add(savedCashTransaction);

            //  Tạo transaction cho QR payment
            var qrTransaction = new Transaction
            {
                OrderId = request.OrderId,
                TransactionCode = $"TXN-QR-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                Amount = request.QrAmount,
                AmountReceived = request.QrAmount, // ✅ FIX: Lưu AmountReceived cho QR transaction (đã được xác nhận thủ công)
                PaymentMethod = "QRBankTransfer",
                Status = "Paid", // Combined payment: QR được xác nhận ngay khi cashier confirm
                CreatedAt = DateTime.Now,
                CompletedAt = DateTime.Now,
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
        transaction.LastRetryAt = DateTime.Now;
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
            transaction.CompletedAt = DateTime.Now;

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
            LockedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.AddMinutes(10)
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
                TransactionCode = $"TXN-SPLIT-{DateTime.Now.Ticks}",
                Amount = totalAmount,
                PaymentMethod = "Split",
                Status = "PartiallyPaid",
                CreatedAt = DateTime.Now,
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
                    TransactionCode = $"TXN-SPLIT-{DateTime.Now.Ticks}-{part.GetHashCode()}",
                    Amount = part.Amount,
                    AmountReceived = part.AmountReceived,
                    PaymentMethod = part.PaymentMethod,
                    Status = part.PaymentMethod == "Cash" && part.AmountReceived.HasValue && part.AmountReceived.Value >= part.Amount
                        ? "Paid"
                        : "PaymentProcessing",
                    CreatedAt = DateTime.Now,
                    CompletedAt = part.PaymentMethod == "Cash" && part.AmountReceived.HasValue && part.AmountReceived.Value >= part.Amount
                        ? DateTime.Now
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
                savedParent.CompletedAt = DateTime.Now;
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

                // ✅ TRIGGER POST-PAYMENT ACTIONS: VIP update, LoyaltyPoints +1, Inventory deduction
                await TriggerPostPaymentActionsAsync(request.OrderId, savedParent.TransactionId, ct);
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
            var transactionCode = $"TXN-{DateTime.Now:yyyyMMddHHmmss}-{orderId}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            var sessionId = Guid.NewGuid().ToString();

            // Create transaction
            var transaction = new Transaction
            {
                OrderId = orderId,
                TransactionCode = transactionCode,
                Amount = totalAmount,
                PaymentMethod = paymentMethod,
                Status = paymentMethod == "Cash" ? "WaitingForPayment" : "PaymentProcessing",
                CreatedAt = DateTime.Now,
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
        transaction.CompletedAt = DateTime.Now;
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
        var cutoffTime = DateTime.Now.AddMinutes(-5);

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
                    // ✅ FIX: Kiểm tra xem Order đã được tăng điểm chưa (tránh duplicate)
                    // Kiểm tra xem đã có audit log "LoyaltyPointsIncreased" cho Order này chưa
                    var existingAuditLogs = await _unitOfWork.AuditLogs.GetByEntityAsync("Order", orderId);
                    var existingLoyaltyLog = existingAuditLogs.FirstOrDefault(a => 
                        a.EventType == "LoyaltyPointsIncreased");
                    
                    if (existingLoyaltyLog != null)
                    {
                        // Đã tăng điểm rồi, bỏ qua
                        await _auditLogService.LogEventAsync(
                            eventType: "LoyaltyPointsSkipped",
                            entityType: "Order",
                            entityId: orderId,
                            description: $"Bỏ qua tăng điểm tích lũy: Order {orderId} đã được tăng điểm trước đó",
                            userId: null,
                            ct: ct);
                        return; // Exit early để tránh duplicate
                    }
                    
                    // ✅ FIX: Fallback sang Reservation.Customer nếu Order.Customer null
                    DomainAccessLayer.Models.Customer? customerToUpdate = null;
                    int customerId = 0;
                    
                    if (order.Customer != null)
                    {
                        customerToUpdate = order.Customer;
                        customerId = order.Customer.CustomerId;
                    }
                    else if (order.Reservation?.Customer != null)
                    {
                        // Fallback: Lấy Customer từ Reservation nếu Order.Customer null
                        customerToUpdate = order.Reservation.Customer;
                        customerId = order.Reservation.Customer.CustomerId;
                    }
                    
                    if (customerToUpdate != null)
                    {
                        // Tăng LoyaltyPoints +1 (nếu null thì set = 1)
                        customerToUpdate.LoyaltyPoints = (customerToUpdate.LoyaltyPoints ?? 0) + 1;
                        
                        // Save changes để lưu LoyaltyPoints
                        await _unitOfWork.SaveChangesAsync();
                        
                        // Log việc tăng điểm (với OrderId để check duplicate sau này)
                        await _auditLogService.LogEventAsync(
                            eventType: "LoyaltyPointsIncreased",
                            entityType: "Order",
                            entityId: orderId,
                            description: $"Tăng điểm tích lũy +1 cho Customer {customerId} sau thanh toán thành công Order {orderId}. Điểm hiện tại: {customerToUpdate.LoyaltyPoints}",
                            userId: null,
                            ct: ct);
                    }
                    else
                    {
                        // Log warning nếu không tìm thấy customer
                        await _auditLogService.LogEventAsync(
                            eventType: "LoyaltyPointsSkipped",
                            entityType: "Order",
                            entityId: orderId,
                            description: $"Không thể tăng điểm tích lũy: Order không có Customer (OrderId: {orderId}, CustomerId: {order.CustomerId}, ReservationId: {order.ReservationId})",
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
                        entityId: order.CustomerId ?? (order.Reservation?.CustomerId ?? 0),
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

        //  QUAN TRỌNG: Giải phóng reserved quantity TRƯỚC KHI cập nhật status
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

    // ========== RESERVATION-CENTRIC PAYMENT METHODS ==========

    /// <summary>
    /// Lấy thông tin thanh toán theo ReservationId (tổng hợp tất cả Orders)
    /// </summary>
    public async Task<ReservationPaymentDto?> GetReservationPaymentAsync(int reservationId, CancellationToken ct = default)
    {
        var orders = await _unitOfWork.Payments.GetOrdersByReservationIdAsync(reservationId);
        var ordersList = orders.ToList();

        if (!ordersList.Any())
        {
            return null;
        }

        // Lấy thông tin Reservation từ order đầu tiên
        var firstOrder = ordersList.First();
        var reservation = firstOrder.Reservation;
        if (reservation == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Reservation với ID: {reservationId}");
        }

        var dto = new ReservationPaymentDto
        {
            ReservationId = reservationId,
            CustomerId = reservation.CustomerId,
            ReservationStatus = reservation.Status,
            CreatedAt = reservation.ReservationTime
        };

        // Lấy thông tin Customer
        var customer = reservation.Customer ?? firstOrder.Customer;
        if (customer != null)
        {
            dto.CustomerId = customer.CustomerId;
            dto.CustomerName = customer.User?.FullName ?? reservation.CustomerNameReservation;
            dto.CustomerPhone = customer.User?.Phone;
            dto.CustomerEmail = customer.User?.Email;
        }

        // Lấy thông tin bàn
        if (reservation.ReservationTables != null && reservation.ReservationTables.Any())
        {
            dto.TableNumbers = reservation.ReservationTables
                .Where(rt => rt.Table != null && !string.IsNullOrEmpty(rt.Table.TableNumber))
                .Select(rt => rt.Table!.TableNumber!)
                .ToList();
            dto.TableNumber = string.Join(", ", dto.TableNumbers);
        }

        // ✅ Lấy WaiterName (người confirm order) từ Orders.ConfirmedByStaffId
        // Lấy từ Order đầu tiên có ConfirmedByStaffId (thường tất cả Orders trong Reservation đều có cùng Waiter)
        var confirmedOrder = ordersList.FirstOrDefault(o => o.ConfirmedByStaffId.HasValue && o.ConfirmedByStaff != null);
        if (confirmedOrder?.ConfirmedByStaff != null)
        {
            dto.WaiterName = confirmedOrder.ConfirmedByStaff.User?.FullName ?? confirmedOrder.ConfirmedByStaff.User.FullName;
        }

        // Map tất cả Orders
        var orderDtos = new List<OrderDto>();
        var allOrderItems = new List<OrderItemDto>();

        foreach (var order in ordersList)
        {
            var orderDto = _mapper.Map<OrderDto>(order);
            CalculateOrderAmounts(order, orderDto);
            PopulateOrderMetadata(order, orderDto);
            orderDtos.Add(orderDto);

            // Phẳng hóa OrderItems để hiển thị
            if (orderDto.OrderItems != null)
            {
                allOrderItems.AddRange(orderDto.OrderItems);
            }
        }

        dto.Orders = orderDtos;
        dto.AllOrderItems = allOrderItems;

        // ✅ Lấy thông tin deposit từ Reservation
        if (reservation != null)
        {
            // Ưu tiên lấy tổng tiền cọc đã thanh toán, fallback về DepositAmount cũ nếu cần
            var deposit = reservation.TotalDepositPaid
                          ?? reservation.DepositAmount
                          ?? 0;

            dto.DepositAmount = deposit;
            dto.DepositPaid = reservation.DepositPaid;
        }

        // ✅ Lấy tất cả Transactions của Reservation
        var transactions = await _unitOfWork.Payments.GetTransactionsByReservationIdAsync(reservationId);
        var transactionDtos = transactions.Select(t => _mapper.Map<TransactionDto>(t)).ToList();
        dto.Transactions = transactionDtos;
        
        // ✅ FIX: Lấy StaffName (Thu ngân xử lý) từ Transaction.ConfirmedByUser (không phải từ Reservation.Staff)
        var paidTransactions = transactions
            .Where(t =>
                string.Equals(t.Status, "Success", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t.Status, "Paid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.CompletedAt ?? t.CreatedAt)
            .ToList();
        
        if (paidTransactions.Any())
        {
            var latestPaidTransaction = paidTransactions.FirstOrDefault();
            if (latestPaidTransaction?.ConfirmedByUser != null)
            {
                dto.StaffName = latestPaidTransaction.ConfirmedByUser.FullName;
            }
        }
        
        // Fallback: Nếu không có StaffName từ transaction, thử lấy từ Reservation.Staff (legacy)
        if (string.IsNullOrWhiteSpace(dto.StaffName) && reservation.Staff != null)
        {
            dto.StaffName = reservation.Staff.FullName;
        }

        // Tính tổng tiền từ tất cả Orders
        CalculateReservationAmount(dto);

        return dto;
    }

    /// <summary>
    /// Tính tổng tiền cho Reservation (tổng hợp từ tất cả Orders)
    /// </summary>
    private void CalculateReservationAmount(ReservationPaymentDto dto)
    {
        // Tính subtotal từ tất cả OrderDetails của tất cả Orders
        decimal subtotal = 0;
        decimal totalDiscount = 0;

        foreach (var order in dto.Orders)
        {
            // Cộng subtotal từ mỗi order
            if (order.Subtotal.HasValue)
            {
                subtotal += order.Subtotal.Value;
            }

            // Cộng discount từ mỗi order
            if (order.DiscountAmount.HasValue)
            {
                totalDiscount += order.DiscountAmount.Value;
            }
        }

        // ✅ FIX: KHÔNG làm tròn Subtotal - giữ nguyên giá trị tính toán
        dto.Subtotal = subtotal;

        // ✅ FIX: Tính VAT (10%) từ Subtotal (KHÔNG làm tròn)
        dto.VatAmount = dto.Subtotal * 0.1m;

        // ✅ FIX: Tính phí dịch vụ (5%) từ Subtotal (KHÔNG làm tròn)
        dto.ServiceFee = dto.Subtotal * 0.05m;

        // ✅ FIX: KHÔNG làm tròn discount - giữ nguyên giá trị tính toán
        dto.DiscountAmount = totalDiscount;

        // Tính tổng tiền cuối cùng (trước khi trừ deposit) - KHÔNG làm tròn các thành phần trung gian
        decimal totalBeforeDeposit = dto.Subtotal + dto.VatAmount + dto.ServiceFee - dto.DiscountAmount;

        // ✅ XỬ LÝ DEPOSIT LOGIC (giống như trong CalculateOrderAmounts)
        decimal depositToDeduct = 0;
        if (dto.DepositPaid == true && dto.DepositAmount.HasValue && dto.DepositAmount.Value > 0)
        {
            depositToDeduct = dto.DepositAmount.Value;
        }

        // ✅ XỬ LÝ TRƯỜNG HỢP DEPOSIT > TOTAL
        if (depositToDeduct > 0 && depositToDeduct > totalBeforeDeposit)
        {
            // Tiền cọc lớn hơn tổng tiền → cần trả lại tiền thừa (KHÔNG làm tròn)
            dto.DepositRefundAmount = depositToDeduct - totalBeforeDeposit;
            dto.TotalAmount = 0; // Không cần thanh toán thêm
        }
        else if (depositToDeduct > 0)
        {
            // ✅ CHỈ làm tròn số tiền cuối cùng khách phải trả (TotalAmount)
            dto.TotalAmount = RoundUpToThousand(totalBeforeDeposit - depositToDeduct);
            dto.DepositRefundAmount = 0; // Không có tiền thừa
        }
        else
        {
            // ✅ CHỈ làm tròn số tiền cuối cùng khách phải trả (TotalAmount)
            dto.TotalAmount = RoundUpToThousand(totalBeforeDeposit);
            dto.DepositRefundAmount = 0;
        }
    }

    /// <summary>
    /// Xử lý thanh toán tiền mặt theo ReservationId
    /// </summary>
    public async Task<TransactionDto> ProcessCashPaymentByReservationAsync(int reservationId, decimal amountReceived, string? notes, int userId, CancellationToken ct = default)
    {
        // Lấy thông tin Reservation payment
        var reservationPayment = await GetReservationPaymentAsync(reservationId, ct);
        if (reservationPayment == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Reservation với ID: {reservationId}");
        }

        // Validate: Tất cả Orders phải đã được xác nhận
        var unconfirmedOrders = reservationPayment.Orders.Where(o => 
            string.IsNullOrEmpty(o.Status) || 
            !o.Status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase)).ToList();

        if (unconfirmedOrders.Any())
        {
            throw new InvalidOperationException(
                $"Có {unconfirmedOrders.Count} đơn hàng chưa được xác nhận. Vui lòng xác nhận tất cả đơn hàng trước khi thanh toán.");
        }

        var totalAmount = reservationPayment.TotalAmount;

        // Validate amount received
        if (amountReceived < totalAmount)
        {
            throw new InvalidOperationException(
                $"⚠️ Số tiền chưa đủ. Tổng tiền: {totalAmount:N0} VND, Nhận được: {amountReceived:N0} VND. Vui lòng kiểm tra lại.");
        }

        // Tính tiền thối
        var refundAmount = amountReceived > totalAmount ? amountReceived - totalAmount : 0;

        // Lock tất cả Orders trong Reservation
        foreach (var order in reservationPayment.Orders)
        {
            await LockOrderAsync(new OrderLockRequestDto { OrderId = order.OrderId }, userId, ct);
        }

        try
        {
            // Tạo Transaction gắn với Reservation (sẽ lưu OrderId của order đầu tiên để backward compatible)
            var firstOrderId = reservationPayment.Orders.First().OrderId;
            var transaction = new Transaction
            {
                OrderId = firstOrderId, // Backward compatible: vẫn lưu OrderId
                ReservationId = reservationId, // ✅ MỚI: Gắn Transaction với Reservation
                TransactionCode = $"TXN-RES-{DateTime.Now:yyyyMMddHHmmss}-{reservationId}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                Amount = totalAmount,
                AmountReceived = amountReceived,
                RefundAmount = refundAmount,
                PaymentMethod = "Cash",
                Status = "Paid",
                CreatedAt = DateTime.Now,
                CompletedAt = DateTime.Now,
                IsManualConfirmed = true,
                ConfirmedByUserId = userId,
                Notes = notes ?? $"Thanh toán tiền mặt cho Reservation {reservationId}. Tổng: {totalAmount:N0} VND"
            };

            var savedTransaction = await _unitOfWork.Payments.SaveTransactionAsync(transaction);

            // Cập nhật trạng thái tất cả Orders thành "Paid"
            foreach (var orderDto in reservationPayment.Orders)
            {
                var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(orderDto.OrderId);
                if (order != null)
                {
                    order.Status = OrderStatusConstants.Paid;
                    order.TotalAmount = orderDto.TotalAmount;
                    await _unitOfWork.Payments.UpdateAsync(order);
                }
            }

            // Cập nhật trạng thái Reservation thành "Completed"
            var reservation = await _unitOfWork.Reservations.GetReservationByIdAsync(reservationId);
            if (reservation != null)
            {
                reservation.Status = "Completed";
                await _unitOfWork.Reservations.UpdateAsync(reservation);
            }

            // Log audit
            await _auditLogService.LogEventAsync(
                "payment_success_by_reservation",
                "Reservation",
                reservationId,
                $"Thanh toán tiền mặt thành công cho Reservation {reservationId}. Tổng: {totalAmount:N0} VND. Số Orders: {reservationPayment.OrderCount}",
                null,
                userId,
                null,
                ct
            );

            // 🔓 GIẢI PHÓNG BÀN VÀ HOÀN THÀNH RESERVATION
            // Release tables (sử dụng order đầu tiên để backward compatible)
            await ReleaseTablesAndCompleteReservationAsync(firstOrderId, userId, ct);

            // Trigger post-payment actions cho tất cả Orders
            foreach (var orderDto in reservationPayment.Orders)
            {
                await TriggerPostPaymentActionsAsync(orderDto.OrderId, savedTransaction.TransactionId, ct);
            }

            // Unlock tất cả Orders
            foreach (var orderDto in reservationPayment.Orders)
            {
                await UnlockOrderAsync(orderDto.OrderId, ct);
            }

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<TransactionDto>(savedTransaction);
        }
        catch
        {
            // Unlock tất cả Orders nếu có lỗi
            foreach (var orderDto in reservationPayment.Orders)
            {
                await UnlockOrderAsync(orderDto.OrderId, ct);
            }
            throw;
        }
    }

    /// <summary>
    /// Xử lý thanh toán QR theo ReservationId (tương tự ProcessCashPaymentByReservationAsync)
    /// </summary>
    public async Task<TransactionDto> ProcessQrPaymentByReservationAsync(int reservationId, string? notes, int userId, CancellationToken ct = default)
    {
        // Lấy thông tin Reservation payment
        var reservationPayment = await GetReservationPaymentAsync(reservationId, ct);
        if (reservationPayment == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Reservation với ID: {reservationId}");
        }

        // Validate: Tất cả Orders phải đã được xác nhận
        var unconfirmedOrders = reservationPayment.Orders.Where(o => 
            string.IsNullOrEmpty(o.Status) || 
            !o.Status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase)).ToList();

        if (unconfirmedOrders.Any())
        {
            throw new InvalidOperationException(
                $"Có {unconfirmedOrders.Count} đơn hàng chưa được xác nhận. Vui lòng xác nhận tất cả đơn hàng trước khi thanh toán.");
        }

        var totalAmount = reservationPayment.TotalAmount;

        // Lock tất cả Orders trong Reservation
        foreach (var order in reservationPayment.Orders)
        {
            await LockOrderAsync(new OrderLockRequestDto { OrderId = order.OrderId }, userId, ct);
        }

        try
        {
            // Tạo Transaction gắn với Reservation (sẽ lưu OrderId của order đầu tiên để backward compatible)
            var firstOrderId = reservationPayment.Orders.First().OrderId;
            var transaction = new Transaction
            {
                OrderId = firstOrderId, // Backward compatible: vẫn lưu OrderId
                ReservationId = reservationId, // ✅ MỚI: Gắn Transaction với Reservation
                TransactionCode = $"TXN-RES-QR-{DateTime.Now:yyyyMMddHHmmss}-{reservationId}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                Amount = totalAmount,
                AmountReceived = totalAmount, // QR payment: received = amount (no change)
                RefundAmount = 0,
                PaymentMethod = "QRBankTransfer",
                Status = "Paid",
                CreatedAt = DateTime.Now,
                CompletedAt = DateTime.Now,
                IsManualConfirmed = true,
                ConfirmedByUserId = userId,
                Notes = notes ?? $"Thanh toán QR cho Reservation {reservationId}. Tổng: {totalAmount:N0} VND"
            };

            var savedTransaction = await _unitOfWork.Payments.SaveTransactionAsync(transaction);

            // Cập nhật trạng thái tất cả Orders thành "Paid"
            foreach (var orderDto in reservationPayment.Orders)
            {
                var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(orderDto.OrderId);
                if (order != null)
                {
                    order.Status = OrderStatusConstants.Paid;
                    order.TotalAmount = orderDto.TotalAmount;
                    await _unitOfWork.Payments.UpdateAsync(order);
                }
            }

            // Cập nhật trạng thái Reservation thành "Completed"
            var reservation = await _unitOfWork.Reservations.GetReservationByIdAsync(reservationId);
            if (reservation != null)
            {
                reservation.Status = "Completed";
                await _unitOfWork.Reservations.UpdateAsync(reservation);
            }

            // Log audit
            await _auditLogService.LogEventAsync(
                "payment_success_by_reservation_qr",
                "Reservation",
                reservationId,
                $"Thanh toán QR thành công cho Reservation {reservationId}. Tổng: {totalAmount:N0} VND. Số Orders: {reservationPayment.OrderCount}",
                null,
                userId,
                null,
                ct
            );

            // 🔓 GIẢI PHÓNG BÀN VÀ HOÀN THÀNH RESERVATION
            // Release tables (sử dụng order đầu tiên để backward compatible)
            await ReleaseTablesAndCompleteReservationAsync(firstOrderId, userId, ct);

            // Trigger post-payment actions cho tất cả Orders
            foreach (var orderDto in reservationPayment.Orders)
            {
                await TriggerPostPaymentActionsAsync(orderDto.OrderId, savedTransaction.TransactionId, ct);
            }

            // Unlock tất cả Orders
            foreach (var orderDto in reservationPayment.Orders)
            {
                await UnlockOrderAsync(orderDto.OrderId, ct);
            }

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<TransactionDto>(savedTransaction);
        }
        catch
        {
            // Unlock tất cả Orders nếu có lỗi
            foreach (var orderDto in reservationPayment.Orders)
            {
                await UnlockOrderAsync(orderDto.OrderId, ct);
            }
            throw;
        }
    }

    /// <summary>
    /// Xử lý thanh toán kết hợp (Cash + QR) theo ReservationId
    /// </summary>
    public async Task<List<TransactionDto>> ProcessCombinedPaymentByReservationAsync(
        int reservationId,
        decimal cashAmount,
        decimal qrAmount,
        decimal? cashReceived,
        string? notes,
        int userId,
        CancellationToken ct = default)
    {
        // Lấy thông tin Reservation payment
        var reservationPayment = await GetReservationPaymentAsync(reservationId, ct);
        if (reservationPayment == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Reservation với ID: {reservationId}");
        }

        // Validate: Tất cả Orders phải đã được xác nhận
        var unconfirmedOrders = reservationPayment.Orders.Where(o => 
            string.IsNullOrEmpty(o.Status) || 
            !o.Status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase)).ToList();

        if (unconfirmedOrders.Any())
        {
            throw new InvalidOperationException(
                $"Có {unconfirmedOrders.Count} đơn hàng chưa được xác nhận. Vui lòng xác nhận tất cả đơn hàng trước khi thanh toán.");
        }

        var totalAmount = reservationPayment.TotalAmount;

        // Validate amounts
        if (cashAmount <= 0 || qrAmount <= 0)
        {
            throw new InvalidOperationException("Số tiền tiền mặt và QR phải lớn hơn 0");
        }

        var partsTotal = cashAmount + qrAmount;
        if (Math.Abs(partsTotal - totalAmount) > 0.01m) // Cho phép sai số nhỏ do làm tròn
        {
            throw new InvalidOperationException(
                $"Tổng hai phần thanh toán ({partsTotal:N0} VND) không khớp với tổng đơn hàng ({totalAmount:N0} VND)");
        }

        // Validate cash received
        var actualCashReceived = cashReceived ?? cashAmount;
        if (actualCashReceived < cashAmount)
        {
            throw new InvalidOperationException(
                $"Số tiền khách đưa ({actualCashReceived:N0} VND) nhỏ hơn phần thanh toán tiền mặt ({cashAmount:N0} VND)");
        }

        // Lock tất cả Orders trong Reservation
        foreach (var order in reservationPayment.Orders)
        {
            await LockOrderAsync(new OrderLockRequestDto { OrderId = order.OrderId }, userId, ct);
        }

        try
        {
            var transactions = new List<Transaction>();
            var firstOrderId = reservationPayment.Orders.First().OrderId;

            // Tính tiền thối cho phần cash
            decimal? cashRefundAmount = null;
            if (actualCashReceived > cashAmount)
            {
                cashRefundAmount = RoundUpToThousand(actualCashReceived - cashAmount);
            }

            // Tạo transaction cho Cash payment
            var cashTransaction = new Transaction
            {
                OrderId = firstOrderId, // Backward compatible
                ReservationId = reservationId, // ✅ MỚI: Gắn Transaction với Reservation
                TransactionCode = $"TXN-RES-CASH-{DateTime.Now:yyyyMMddHHmmss}-{reservationId}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                Amount = cashAmount,
                AmountReceived = actualCashReceived,
                RefundAmount = cashRefundAmount,
                PaymentMethod = "Cash",
                Status = "Paid",
                CreatedAt = DateTime.Now,
                CompletedAt = DateTime.Now,
                IsManualConfirmed = true,
                ConfirmedByUserId = userId,
                Notes = $"Thanh toán kết hợp - Phần tiền mặt: {cashAmount:N0} VND" + 
                        (cashRefundAmount.HasValue ? $", Tiền thối: {cashRefundAmount.Value:N0} VND" : "")
            };

            var savedCashTransaction = await _unitOfWork.Payments.SaveTransactionAsync(cashTransaction);
            transactions.Add(savedCashTransaction);

            // Tạo transaction cho QR payment
            var qrTransaction = new Transaction
            {
                OrderId = firstOrderId, // Backward compatible
                ReservationId = reservationId, // ✅ MỚI: Gắn Transaction với Reservation
                TransactionCode = $"TXN-RES-QR-{DateTime.Now:yyyyMMddHHmmss}-{reservationId}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                Amount = qrAmount,
                AmountReceived = qrAmount,
                RefundAmount = 0,
                PaymentMethod = "QRBankTransfer",
                Status = "Paid",
                CreatedAt = DateTime.Now,
                CompletedAt = DateTime.Now,
                IsManualConfirmed = true,
                ConfirmedByUserId = userId,
                Notes = $"Thanh toán kết hợp - Phần QR: {qrAmount:N0} VND"
            };

            var savedQrTransaction = await _unitOfWork.Payments.SaveTransactionAsync(qrTransaction);
            transactions.Add(savedQrTransaction);

            // Cập nhật trạng thái tất cả Orders thành "Paid"
            foreach (var orderDto in reservationPayment.Orders)
            {
                var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(orderDto.OrderId);
                if (order != null)
                {
                    order.Status = OrderStatusConstants.Paid;
                    order.TotalAmount = orderDto.TotalAmount;
                    await _unitOfWork.Payments.UpdateAsync(order);
                }
            }

            // Cập nhật trạng thái Reservation thành "Completed"
            var reservation = await _unitOfWork.Reservations.GetReservationByIdAsync(reservationId);
            if (reservation != null)
            {
                reservation.Status = "Completed";
                await _unitOfWork.Reservations.UpdateAsync(reservation);
            }

            // Log audit
            await _auditLogService.LogEventAsync(
                "combined_payment_success_by_reservation",
                "Reservation",
                reservationId,
                $"Thanh toán kết hợp thành công cho Reservation {reservationId}. Cash: {cashAmount:N0} VND, QR: {qrAmount:N0} VND. Tổng: {totalAmount:N0} VND. Số Orders: {reservationPayment.OrderCount}",
                null,
                userId,
                null,
                ct
            );

            // 🔓 GIẢI PHÓNG BÀN VÀ HOÀN THÀNH RESERVATION
            await ReleaseTablesAndCompleteReservationAsync(firstOrderId, userId, ct);

            // Trigger post-payment actions cho tất cả Orders
            foreach (var orderDto in reservationPayment.Orders)
            {
                await TriggerPostPaymentActionsAsync(orderDto.OrderId, savedCashTransaction.TransactionId, ct);
            }

            // Unlock tất cả Orders
            foreach (var orderDto in reservationPayment.Orders)
            {
                await UnlockOrderAsync(orderDto.OrderId, ct);
            }

            await _unitOfWork.SaveChangesAsync();

            return transactions.Select(t => _mapper.Map<TransactionDto>(t)).ToList();
        }
        catch
        {
            // Unlock tất cả Orders nếu có lỗi
            foreach (var orderDto in reservationPayment.Orders)
            {
                await UnlockOrderAsync(orderDto.OrderId, ct);
            }
            throw;
        }
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

        //  QUAN TRỌNG: Giải phóng reserved quantity TRƯỚC KHI cập nhật status
        // Hủy tất cả các món trong đơn (nếu chưa được chế biến)
        if (order.OrderDetails != null)
        {
            var inventoryService = _serviceProvider.GetRequiredService<IInventoryIngredientService>();
            
            foreach (var detail in order.OrderDetails)
            {
                var status = (detail.Status ?? "").Trim().ToLower();
                if (status == "pending" || status == "confirmed" || status == "đã gửi" || status == "cooking")
                {
                    //  Giải phóng reserved quantity TRƯỚC KHI set status = Cancelled
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

