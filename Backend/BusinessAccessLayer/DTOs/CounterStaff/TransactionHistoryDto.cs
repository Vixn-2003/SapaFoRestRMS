using System;
using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.CounterStaff
{
    /// <summary>
    /// DTO cho Transaction History - UC124
    /// </summary>
    public class TransactionHistoryDto
    {
        public int TransactionId { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public int? ReservationId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string TableNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO cho filter transaction history
    /// </summary>
    public class TransactionFilterDto
    {
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// DTO cho export Excel
    /// </summary>
    public class TransactionExcelDto
    {
        public string TransactionCode { get; set; } = string.Empty;
        public string OrderCode { get; set; } = string.Empty;
        public int? ReservationId { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO trả về danh sách transactions có phân trang
    /// </summary>
    public class TransactionHistoryListDto
    {
        public List<TransactionHistoryDto> Transactions { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}

