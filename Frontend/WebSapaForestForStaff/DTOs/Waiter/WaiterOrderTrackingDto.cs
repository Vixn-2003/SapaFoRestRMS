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
    }
}

