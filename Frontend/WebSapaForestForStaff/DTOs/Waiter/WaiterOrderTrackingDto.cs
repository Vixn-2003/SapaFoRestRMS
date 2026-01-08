using System;
using System.Collections.Generic;

namespace WebSapaForestForStaff.DTOs.Waiter
{
    public class WaiterOrderTrackingDto
    {
        public int ProcessingCount { get; set; }
        public int WaitingKitchenCount { get; set; }
        public int CookingCount { get; set; }
        public int ReadyCount { get; set; }
        public int TotalCount { get; set; }
        public List<OrderTrackingGroupDto> OrderGroups { get; set; } = new();
    }

    public class OrderTrackingGroupDto
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string AreaName { get; set; } = string.Empty;
        public string TableNumber { get; set; } = string.Empty;
        public int NumberOfGuests { get; set; }
        public List<OrderTrackingItemDto> Items { get; set; } = new();
    }

    public class OrderTrackingItemDto
    {
        public int OrderDetailId { get; set; }
        /// <summary>
        /// OrderComboItemId - null nếu là món lẻ, có giá trị nếu là món trong combo
        /// </summary>
        public int? OrderComboItemId { get; set; }
        public int OrderId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool IsUrgent { get; set; }
        public string? UrgentReason { get; set; }
        public DateTime OrderTime { get; set; }
        public int WaitingMinutes { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? ReadyAt { get; set; }
        public DateTime? ServedAt { get; set; }
        public bool CanCancel { get; set; }
        public bool CanReturn { get; set; }
        public bool CanRequestUrgent { get; set; }
        public bool IsSplit { get; set; } // Đã được tách từ order detail gốc (bếp phó nấu một phần)
        
        /// <summary>
        /// BillingType của món: 0=Unspecified, 1=ConsumptionBased, 2=KitchenPrepared
        /// Món có BillingType = 1 (ConsumptionBased) có thể tăng/giảm số lượng kể cả sau khi xác nhận
        /// </summary>
        public int? BillingType { get; set; }
        
        /// <summary>
        /// True nếu là món ConsumptionBased (BillingType = 1) - có thể tăng/giảm số lượng
        /// </summary>
        public bool IsConsumptionBased => BillingType == 1;
        
        /// <summary>
        /// Số lượng thực tế đã sử dụng (cho ConsumptionBased items)
        /// </summary>
        public int? QuantityUsed { get; set; }
        
        /// <summary>
        /// True nếu có thể xác nhận số lượng ngay (cho món ConsumptionBased, không cần chờ Ready)
        /// </summary>
        public bool CanConfirmQuantity { get; set; }
    }
}

