using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.DTOs.Payment
{
    public class SplitBillRequest
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        [MinLength(2, ErrorMessage = "Cần ít nhất 2 phần khi chia hóa đơn.")]
        public List<SplitBillPart> Parts { get; set; } = new();

        public string? Notes { get; set; }
    }

    public class SplitBillPart
    {
        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
        public decimal Amount { get; set; }

        public decimal? AmountReceived { get; set; }

        public string? Notes { get; set; }
    }

    public class SplitBillResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public bool AllPaid { get; set; }
        public List<TransactionDto>? Transactions { get; set; }
    }

    public class TransactionDto
    {
        public int TransactionId { get; set; }
        public int? ParentTransactionId { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public decimal? AmountReceived { get; set; }
        public decimal? RefundAmount { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Notes { get; set; }
    }
}


