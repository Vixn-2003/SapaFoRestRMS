using System;
using System.Collections.Generic;

namespace WebSapaForestForStaff.DTOs.Kitchen
{
    /// <summary>
    /// Response for station items by category
    /// </summary>
    public class StationItemsResponse
    {
        public string CategoryName { get; set; } = string.Empty;
        public List<StationItemDto> AllItems { get; set; } = new();
        public List<StationItemDto> UrgentItems { get; set; } = new();
    }

    /// <summary>
    /// DTO for station item
    /// </summary>
    public class StationItemDto
    {
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string TableNumber { get; set; } = string.Empty;
        public string MenuItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = "Pending";
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedAtTime { get; set; } = string.Empty; // Format: HH:mm
        public int WaitingMinutes { get; set; }
        public bool IsUrgent { get; set; }
        public DateTime? StartedAt { get; set; }
        public string FireTime { get; set; } = string.Empty; // Format: HH:mm
        public int? TimeCook { get; set; }
        public int? BatchSize { get; set; }
    }
}

