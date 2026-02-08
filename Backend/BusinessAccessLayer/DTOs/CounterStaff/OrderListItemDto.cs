using System;

namespace BusinessAccessLayer.DTOs.CounterStaff
{
    /// <summary>
    /// DTO cho Order trong danh sách order - UC123
    /// </summary>
    public class OrderListItemDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string TableNumber { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public decimal? TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsWaiterConfirmed { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int? NumberOfGuests { get; set; }
    }

    /// <summary>
    /// DTO cho filter order list
    /// </summary>
    public class OrderListFilterDto
    {
        public string? Status { get; set; }
        public DateOnly? Date { get; set; }
        public string? TableNumber { get; set; }
        public string? SearchKeyword { get; set; }
    }
}

