using AutoMapper;
using BusinessAccessLayer.DTOs.Payment;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services;

/// <summary>
/// Service xử lý business logic cho Payment
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PaymentService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<OrderDto>> GetPendingOrdersAsync(CancellationToken ct = default)
    {
        var orders = await _unitOfWork.Payments.GetPendingOrdersAsync();
        var orderDtos = new List<OrderDto>();

        foreach (var order in orders)
        {
            var orderDto = _mapper.Map<OrderDto>(order);
            
            // Tính toán lại tổng tiền nếu cần
            CalculateOrderAmounts(order, orderDto);
            
            // Lấy thông tin bàn nếu có
            if (order.Reservation != null && order.Reservation.ReservationTables != null && order.Reservation.ReservationTables.Any())
            {
                // Lấy TableNumber từ ReservationTables (có thể có nhiều bàn)
                var tableNumbers = order.Reservation.ReservationTables
                    .Where(rt => rt.Table != null)
                    .Select(rt => rt.Table.TableNumber)
                    .ToList();
                
                orderDto.TableNumber = string.Join(", ", tableNumbers); // Nếu có nhiều bàn, join bằng dấu phẩy
            }

            orderDtos.Add(orderDto);
        }

        return orderDtos;
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

        // Lấy thông tin bàn và khách hàng
        if (order.Reservation != null && order.Reservation.ReservationTables != null && order.Reservation.ReservationTables.Any())
        {
            // Lấy TableNumber từ ReservationTables (có thể có nhiều bàn)
            var tableNumbers = order.Reservation.ReservationTables
                .Where(rt => rt.Table != null)
                .Select(rt => rt.Table.TableNumber)
                .ToList();
            
            orderDto.TableNumber = string.Join(", ", tableNumbers); // Nếu có nhiều bàn, join bằng dấu phẩy
        }

        if (order.Customer != null)
        {
            orderDto.CustomerName = order.Customer.User.FullName;
        }

        return orderDto;
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

    public async Task<TransactionDto> ProcessPaymentAsync(PaymentRequestDto request, CancellationToken ct = default)
    {
        var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(request.OrderId);
        
        if (order == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {request.OrderId}");
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
        order.Status = "Paid";
        await _unitOfWork.Payments.UpdateAsync(order);

        // Lưu transaction
        var savedTransaction = await _unitOfWork.Payments.SaveTransactionAsync(transaction);
        
        // Save changes
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<TransactionDto>(savedTransaction);
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
        // Tính subtotal từ OrderDetails
        decimal subtotal = 0;
        if (order.OrderDetails != null && order.OrderDetails.Any())
        {
            subtotal = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity);
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
}

