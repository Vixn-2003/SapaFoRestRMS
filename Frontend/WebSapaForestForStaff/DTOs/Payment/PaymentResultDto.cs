namespace WebSapaForestForStaff.DTOs.Payment
{
    public class PaymentResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public string? TransactionCode { get; set; }
        public string? SessionId { get; set; }
    }
}

