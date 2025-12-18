using System;
using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.CustomerManagement
{
    /// <summary>
    /// DTO cho chi tiết customer trong UC146 - View Customer Detail
    /// </summary>
    public class CustomerDetailDto
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateTime? JoinDate { get; set; }
        public decimal TotalSpending { get; set; }
        public int TotalVisits { get; set; }
        public decimal AverageSpendPerVisit { get; set; }
        public DateTime? LastVisit { get; set; }
        public int? LoyaltyPoints { get; set; }
        public bool IsVip { get; set; }
        public string? Notes { get; set; }
        
        /// <summary>
        /// Top 3 món ăn yêu thích
        /// </summary>
        public List<FavoriteDishDto> FavoriteDishes { get; set; } = new();
        
        /// <summary>
        /// Lịch sử đơn hàng
        /// </summary>
        public List<CustomerOrderSummaryDto> OrderHistory { get; set; } = new();
        
        /// <summary>
        /// Spending trend - dùng để vẽ chart (optional)
        /// </summary>
        public List<MonthlySpendingDto>? SpendingTrend { get; set; }
    }
    
    /// <summary>
    /// Món ăn yêu thích
    /// </summary>
    public class FavoriteDishDto
    {
        public int MenuItemId { get; set; }
        public string DishName { get; set; } = null!;
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
    }
    
    /// <summary>
    /// Tóm tắt đơn hàng của customer
    /// </summary>
    public class CustomerOrderSummaryDto
    {
        public int OrderId { get; set; }
        public DateTime? OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public int NumberOfItems { get; set; }
        public string OrderType { get; set; } = null!;
        public int? PaymentId { get; set; }
    }
    
    /// <summary>
    /// Chi tiêu theo tháng (optional - cho chart)
    /// </summary>
    public class MonthlySpendingDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalSpent { get; set; }
        public int VisitCount { get; set; }
    }
}

