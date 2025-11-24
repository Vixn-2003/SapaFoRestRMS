namespace WebSapaForestForStaff.DTOs.Payment
{
    /// <summary>
    /// Thông tin session thanh toán trả về từ initiate payment
    /// </summary>
    public class PaymentSessionDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string? QrCodeUrl { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }
    }
}

