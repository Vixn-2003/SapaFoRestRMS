namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho xác nhận thanh toán QR theo ReservationId
/// </summary>
public class ReservationQrPaymentRequestDto
{
    public string? Notes { get; set; }
}

