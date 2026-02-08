namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho response VietQR
/// </summary>
public class VietQRResponseDto
{
    public string QrUrl { get; set; } = null!;
    public int OrderId { get; set; }
    public decimal Total { get; set; }
    public string OrderCode { get; set; } = null!;
    public string Description { get; set; } = null!;
}

