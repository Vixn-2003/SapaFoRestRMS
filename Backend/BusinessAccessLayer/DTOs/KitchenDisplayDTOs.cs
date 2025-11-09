using System;
using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.Kitchen
{
    /// <summary>
    /// DTO for Kitchen Display System - represents one order card
    /// </summary>
    public class KitchenOrderCardDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } // "A01", "A02"...
        public string TableNumber { get; set; } // Tên nhân viên hoặc số bàn
        public string StaffName { get; set; } // Tên nhân viên đã order (mới thêm)
        public DateTime CreatedAt { get; set; }
        public int WaitingMinutes { get; set; } // Calculated: now - CreatedAt
        public string PriorityLevel { get; set; } // "Normal", "Warning", "Critical"
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

    /// <summary>
    /// Request to update item status from station screen
    /// </summary>
    public class UpdateItemStatusRequest
    {
        public int OrderDetailId { get; set; }
        public string NewStatus { get; set; } = string.Empty; // "Cooking" or "Done"
        public int UserId { get; set; } // Who pressed the button
    }

    /// <summary>
    /// Request to complete entire order (from Sous Chef)
    /// </summary>
    public class CompleteOrderRequest
    {
        public int OrderId { get; set; }
        public int SousChefUserId { get; set; }
    }

    /// <summary>
    /// Response after status update
    /// </summary>
    public class StatusUpdateResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public KitchenOrderItemDto? UpdatedItem { get; set; }
    }

    /// <summary>
    /// Real-time notification payload
    /// </summary>
    public class KitchenStatusChangeNotification
    {
        public int OrderId { get; set; }
        public int OrderDetailId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for grouped items by menu item (theo từng món)
    /// </summary>
    public class GroupedMenuItemDto
    {
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int TotalQuantity { get; set; } // Tổng số lượng từ tất cả các order
        public string CourseType { get; set; } = string.Empty;
        public List<GroupedItemDetailDto> ItemDetails { get; set; } = new(); // Chi tiết từng order
    }

    /// <summary>
    /// Chi tiết từng item trong grouped menu item
    /// </summary>
    public class GroupedItemDetailDto
    {
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string TableNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = "Pending";
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int WaitingMinutes { get; set; }
    }

    /// <summary>
    /// DTO cho Station screen - mỗi item trong trạm
    /// </summary>
    public class StationItemDto
    {
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string TableNumber { get; set; } = string.Empty;
        public string MenuItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Cooking, Done
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedAtTime { get; set; } = string.Empty; // Format: HH:mm
        public int WaitingMinutes { get; set; }
        public bool IsUrgent { get; set; } // Đánh dấu được yêu cầu từ bếp phó
        public DateTime? StartedAt { get; set; } // Thời gian bắt đầu nấu (khi bếp phó fire)
        public string FireTime { get; set; } = string.Empty; // Format: HH:mm - thời gian fire
    }

    /// <summary>
    /// Response cho Station screen - có 2 luồng
    /// </summary>
    public class StationItemsResponse
    {
        public string CategoryName { get; set; } = string.Empty;
        public List<StationItemDto> AllItems { get; set; } = new(); // Luồng 1: Tất cả orders
        public List<StationItemDto> UrgentItems { get; set; } = new(); // Luồng 2: Orders được yêu cầu
    }

    /// <summary>
    /// Request để đánh dấu order cần làm ngay
    /// </summary>
    public class MarkAsUrgentRequest
    {
        public int OrderDetailId { get; set; }
        public bool IsUrgent { get; set; }
    }
}