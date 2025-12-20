using WebSapaForestForStaff.DTOs.Payment;

namespace WebSapaForestForStaff.Services.Api.Interfaces
{
    public interface IPaymentApiService : IBaseApiService
    {
        Task<List<OrderDto>> GetPendingOrdersAsync();
        Task<List<OrderDto>> GetPaidOrdersAsync();
        Task<List<OrderDto>> GetOrdersByStatusAndDateAsync(string statusFilter, DateOnly date);
        Task<OrderDetailDto?> GetOrderDetailAsync(int orderId);
        Task<BaseApiService.ApiResult> ConfirmCustomerOrderAsync(ConfirmOrderRequest request);
        Task<PaymentSessionDto?> InitiatePaymentAsync(PaymentInitiateRequest request);
        Task<BaseApiService.ApiResult> ConfirmPaymentAsync(PaymentConfirmRequest request);
        Task<ReceiptFileDto?> GenerateReceiptAsync(int orderId);
        Task<ReceiptFileDto?> GenerateReceiptByReservationAsync(int reservationId);
        Task<DiscountApplyResponse?> ApplyDiscountAsync(DiscountRequest request);
        Task<BaseApiService.ApiResult> ProcessCombinedPaymentAsync(CombinedPaymentRequest request);
        Task<BaseApiService.ApiResult> CancelOrderAsync(int orderId, string reason);
        Task<BaseApiService.ApiResult> UndoConfirmOrderAsync(int orderId, UndoConfirmRequest request);

        // ========== RESERVATION-CENTRIC PAYMENT METHODS ==========

        /// <summary>
        /// Lấy thông tin thanh toán theo ReservationId (tổng hợp tất cả Orders)
        /// </summary>
        Task<ReservationPaymentDto?> GetReservationPaymentAsync(int reservationId);

        /// <summary>
        /// Xử lý thanh toán tiền mặt theo ReservationId
        /// </summary>
        Task<BaseApiService.ApiResult> ProcessCashPaymentByReservationAsync(int reservationId, decimal amountReceived, string? notes);

        /// <summary>
        /// Xác nhận thanh toán QR theo ReservationId
        /// </summary>
        Task<BaseApiService.ApiResult> ConfirmQrPaymentByReservationAsync(int reservationId, string? notes);

        /// <summary>
        /// Xử lý thanh toán kết hợp (Cash + QR) theo ReservationId
        /// </summary>
        Task<BaseApiService.ApiResult> ProcessCombinedPaymentByReservationAsync(int reservationId, decimal cashAmount, decimal qrAmount, decimal? cashReceived, string? notes);

        /// <summary>
        /// Xác nhận Reservation (confirm tất cả Orders trong Reservation)
        /// </summary>
        Task<BaseApiService.ApiResult> ConfirmReservationAsync(int reservationId, ReservationConfirmRequest request);

        /// <summary>
        /// Áp dụng ưu đãi/giảm giá cho Reservation (áp dụng cho tất cả Orders trong Reservation)
        /// </summary>
        Task<BaseApiService.ApiResult> ApplyDiscountByReservationAsync(int reservationId, ReservationDiscountRequest request);
    }
}

