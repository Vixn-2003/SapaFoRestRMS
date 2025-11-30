using System;
using System.Collections.Generic;

namespace WebSapaForestForStaff.DTOs.Kitchen
{
    /// <summary>
    /// DTO for grouped items by menu item (theo từng món)
    /// </summary>
    public class GroupedMenuItemDto
    {
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int TotalQuantity { get; set; }
        public string CourseType { get; set; } = string.Empty;
        public int? TimeCook { get; set; }
        public int? BatchSize { get; set; }
        public List<GroupedItemDetailDto> ItemDetails { get; set; } = new();
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
}

