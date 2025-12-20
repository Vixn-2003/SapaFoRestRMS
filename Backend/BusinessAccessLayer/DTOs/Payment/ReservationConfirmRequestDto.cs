using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// Yêu cầu xác nhận Reservation (confirm tất cả Orders trong Reservation)
/// </summary>
public class ReservationConfirmRequestDto
{
    public int ReservationId { get; set; }

    /// <summary>
    /// Danh sách các món cần confirm (từ tất cả Orders trong Reservation)
    /// Key: OrderId, Value: List các OrderDetailId và thông tin confirm
    /// Sử dụng CustomerConfirmedItemDto từ CustomerConfirmRequestDto
    /// </summary>
    public Dictionary<int, List<CustomerConfirmedItemDto>> OrderItems { get; set; } = new();

    public string? Notes { get; set; }
}

