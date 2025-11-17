namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho response kiểm tra trạng thái thanh toán
/// </summary>
public class PaymentStatusResponseDto
{
    public int TransactionId { get; set; }
    public int OrderId { get; set; }
    public string Status { get; set; } = null!;
    public string PaymentMethod { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? GatewayErrorCode { get; set; }
    public string? GatewayErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsManualConfirmed { get; set; }
}

