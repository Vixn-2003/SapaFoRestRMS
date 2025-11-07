using BusinessAccessLayer.DTOs.Payment;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces;

/// <summary>
/// Interface cho Payment Service
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Lấy danh sách đơn hàng chờ thanh toán
    /// </summary>
    Task<IEnumerable<OrderDto>> GetPendingOrdersAsync(CancellationToken ct = default);

    /// <summary>
    /// Lấy chi tiết đơn hàng kèm danh sách món ăn
    /// </summary>
    Task<OrderDto?> GetOrderDetailAsync(int orderId, CancellationToken ct = default);

    /// <summary>
    /// Áp dụng ưu đãi/giảm giá cho đơn hàng
    /// </summary>
    Task<OrderDto> ApplyDiscountAsync(DiscountRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Khởi tạo giao dịch thanh toán
    /// </summary>
    Task<TransactionDto> InitiatePaymentAsync(PaymentInitiateRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Xử lý thanh toán (xác nhận thanh toán)
    /// </summary>
    Task<TransactionDto> ProcessPaymentAsync(PaymentRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Lấy kết quả thanh toán theo sessionId
    /// </summary>
    Task<TransactionDto?> GetPaymentResultAsync(string sessionId, CancellationToken ct = default);
}

