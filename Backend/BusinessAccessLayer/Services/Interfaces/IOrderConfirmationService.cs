using BusinessAccessLayer.DTOs.OrderConfirmation;

namespace BusinessAccessLayer.Services.Interfaces
{
    /// <summary>
    /// Service xử lý logic xác nhận hóa đơn
    /// Phân biệt Kitchen-prepared và Consumption-based items
    /// </summary>
    public interface IOrderConfirmationService
    {
        /// <summary>
        /// Lấy thông tin đơn hàng để xác nhận
        /// </summary>
        Task<OrderConfirmationDto?> GetOrderForConfirmationAsync(int orderId, CancellationToken ct = default);
        
        /// <summary>
        /// Xác nhận hóa đơn với số lượng sử dụng của món consumption
        /// </summary>
        Task<OrderConfirmationDto> ConfirmOrderAsync(ConfirmOrderRequestDto request, CancellationToken ct = default);
        
        /// <summary>
        /// Hủy món (chỉ cho Kitchen items ở trạng thái NotStarted)
        /// </summary>
        Task<bool> CancelItemAsync(CancelItemRequestDto request, CancellationToken ct = default);
        
        /// <summary>
        /// Validate xem món có thể hủy không
        /// </summary>
        Task<(bool CanCancel, string Reason)> ValidateCanCancelItemAsync(int orderDetailId, CancellationToken ct = default);
    }
}

