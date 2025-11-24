namespace WebSapaForestForStaff.DTOs.Payment
{
    /// <summary>
    /// Request DTO cho việc xác nhận thanh toán
    /// </summary>
    public class PaymentConfirmRequest
    {
        public int OrderId { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public decimal? CashGiven { get; set; }
        public string? Notes { get; set; }
    }
}

