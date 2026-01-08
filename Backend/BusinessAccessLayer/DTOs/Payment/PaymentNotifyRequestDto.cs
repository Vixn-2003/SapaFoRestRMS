namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho payment notification từ gateway
/// </summary>
public class PaymentNotifyRequestDto
{
    public string? TransactionCode { get; set; }
    public string? SessionId { get; set; }
    public string Status { get; set; } = null!;
    public decimal? Amount { get; set; }
    public string? GatewayErrorCode { get; set; }
    public string? GatewayErrorMessage { get; set; }
    public string? Signature { get; set; }
    public Dictionary<string, string>? AdditionalData { get; set; }
}

