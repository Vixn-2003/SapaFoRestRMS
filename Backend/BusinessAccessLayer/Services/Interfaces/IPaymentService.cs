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

    /// <summary>
    /// Tạo VietQR URL cho đơn hàng
    /// </summary>
        Task<VietQRResponseDto> GenerateVietQRAsync(int orderId, string bankCode, string account, CancellationToken ct = default);
        Task<VietQRResponseDto> GenerateVietQRAsync(int orderId, string bankCode, string account, decimal? customAmount, CancellationToken ct = default);

    // ========== PHASE 1: Payment Flow Extensions ==========

    /// <summary>
    /// CASE 1: Xử lý thanh toán tiền mặt với validation
    /// </summary>
    Task<TransactionDto> ProcessCashPaymentAsync(CashPaymentRequestDto request, int userId, CancellationToken ct = default);

    /// <summary>
    /// CASE 2: Kiểm tra trạng thái thanh toán
    /// </summary>
    Task<PaymentStatusResponseDto> CheckPaymentStatusAsync(int orderId, CancellationToken ct = default);

    /// <summary>
    /// CASE 3: Retry payment đã thất bại
    /// </summary>
    Task<TransactionDto> RetryPaymentAsync(PaymentRetryRequestDto request, int userId, CancellationToken ct = default);

    /// <summary>
    /// CASE 4: Sync offline payments
    /// </summary>
    Task<List<TransactionDto>> SyncPaymentsAsync(List<int> transactionIds, CancellationToken ct = default);

    /// <summary>
    /// CASE 5: Gateway callback notification
    /// </summary>
    Task<bool> NotifyPaymentAsync(PaymentNotifyRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// CASE 6: Lock order khi payment in progress
    /// </summary>
    Task<bool> LockOrderAsync(OrderLockRequestDto request, int userId, CancellationToken ct = default);

    /// <summary>
    /// CASE 6: Unlock order
    /// </summary>
    Task<bool> UnlockOrderAsync(int orderId, CancellationToken ct = default);

    /// <summary>
    /// CASE 6: Kiểm tra order có đang bị lock không
    /// </summary>
    Task<bool> IsOrderLockedAsync(int orderId, CancellationToken ct = default);

    /// <summary>
    /// CASE 7: Xử lý split bill
    /// </summary>
    Task<List<TransactionDto>> ProcessSplitBillAsync(SplitBillRequestDto request, int userId, CancellationToken ct = default);

    // ========== REVISED PAYMENT WORKFLOW METHODS ==========

    /// <summary>
    /// Bắt đầu thanh toán - tạo transaction và khởi tạo payment flow
    /// GET /api/payments/start/{orderId}
    /// </summary>
    Task<TransactionDto> StartPaymentAsync(int orderId, string paymentMethod, CancellationToken ct = default);

    /// <summary>
    /// Xử lý callback từ payment gateway
    /// POST /api/payments/notify
    /// </summary>
    Task<bool> HandleCallbackAsync(PaymentNotifyRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Xác nhận thanh toán thủ công (cho cash hoặc khi gateway chậm)
    /// POST /api/payments/confirm
    /// </summary>
    Task<TransactionDto> ConfirmManualAsync(PaymentConfirmRequestDto request, int userId, CancellationToken ct = default);

    /// <summary>
    /// Hủy thanh toán
    /// POST /api/payments/cancel
    /// </summary>
    Task<bool> CancelPaymentAsync(PaymentCancelRequestDto request, int userId, CancellationToken ct = default);

    /// <summary>
    /// Retry các transaction đang pending (background job)
    /// </summary>
    Task<List<TransactionDto>> RetryPendingTransactionsAsync(CancellationToken ct = default);
}

