namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho request hủy toàn bộ đơn hàng
/// </summary>
public class CancelOrderRequestDto
{
    /// <summary>
    /// Lý do hủy đơn
    /// </summary>
    public string? Reason { get; set; }
}

