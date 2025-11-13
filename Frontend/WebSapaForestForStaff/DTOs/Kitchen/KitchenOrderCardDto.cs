using System;
using System.Collections.Generic;

namespace WebSapaForestForStaff.DTOs.Kitchen
{
    /// <summary>
    /// DTO for Kitchen Display System - represents one order card
    /// </summary>
    public class KitchenOrderCardDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty; // "A01", "A02"...
        public string TableNumber { get; set; } = string.Empty; // Tên nhân viên hoặc số bàn
        public string StaffName { get; set; } = string.Empty; // Tên nhân viên đã order
        public DateTime CreatedAt { get; set; }
        public int WaitingMinutes { get; set; } // Calculated: now - CreatedAt
        public string PriorityLevel { get; set; } = string.Empty; // "Normal", "Warning", "Critical"
        public int TotalItems { get; set; }
        public int CompletedItems { get; set; }
        public List<KitchenOrderItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// DTO for each menu item in the order
    /// </summary>
    public class KitchenOrderItemDto
    {
        public int OrderDetailId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Cooking, Done
        public string? Notes { get; set; } // Modifier (e.g., "không có tiêu đen")
        public string CourseType { get; set; } = string.Empty; // Trạm nào (Xào, Nướng...)
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsUrgent { get; set; } // Đánh dấu được yêu cầu từ bếp phó
    }
}

