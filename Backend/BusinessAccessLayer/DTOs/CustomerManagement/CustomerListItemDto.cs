using System;

namespace BusinessAccessLayer.DTOs.CustomerManagement
{
    /// <summary>
    /// DTO cho danh sách customer trong UC145 - View List Customer
    /// </summary>
    public class CustomerListItemDto
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public decimal TotalSpending { get; set; }
        public int TotalVisits { get; set; }
        public bool IsVip { get; set; }
        public DateTime? LastVisit { get; set; }
        public int? LoyaltyPoints { get; set; }
        public decimal AverageSpendPerVisit { get; set; }
    }
}

