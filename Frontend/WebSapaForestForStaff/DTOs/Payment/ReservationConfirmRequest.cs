namespace WebSapaForestForStaff.DTOs.Payment
{
    /// <summary>
    /// Request DTO cho việc xác nhận Reservation (confirm tất cả Orders trong Reservation)
    /// </summary>
    public class ReservationConfirmRequest
    {
        public int ReservationId { get; set; }

        /// <summary>
        /// Danh sách các món cần confirm (từ tất cả Orders trong Reservation)
        /// Key: OrderId, Value: List các OrderDetailId và thông tin confirm
        /// Sử dụng ConfirmedItemDto từ ConfirmOrderRequest
        /// </summary>
        public Dictionary<int, List<ConfirmedItemDto>>? OrderItems { get; set; }

        public string? Notes { get; set; }
    }
}

