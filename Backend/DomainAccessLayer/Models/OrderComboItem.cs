using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

/// <summary>
/// Bảng lưu trữ trạng thái của từng món con trong Combo đã được order
/// Cho phép quản lý status riêng biệt cho từng món (ví dụ: Burger đang nướng, Coca đã xong)
/// </summary>
public partial class OrderComboItem
{
    public int OrderComboItemId { get; set; }

    /// <summary>
    /// Foreign key trỏ về OrderDetails - để biết món này thuộc dòng order nào (dòng Combo)
    /// </summary>
    public int OrderDetailId { get; set; }

    /// <summary>
    /// Foreign key trỏ về MenuItems - để biết đây là món gì (ví dụ: Burger, Coca)
    /// </summary>
    public int MenuItemId { get; set; }

    /// <summary>
    /// Trạng thái của món con trong combo (Pending, Cooking, Ready, Served, etc.)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Số lượng món này trong combo (nếu trong combo có 2 cái burger)
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Thời gian bắt đầu nấu (khi status chuyển sang "Cooking")
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Thời gian món được đánh dấu "Sẵn sàng" (Ready)
    /// </summary>
    public DateTime? ReadyAt { get; set; }

    /// <summary>
    /// Ghi chú riêng cho món này (ví dụ: Burger không hành)
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Đánh dấu món được yêu cầu làm ngay
    /// </summary>
    public bool IsUrgent { get; set; } = false;

    /// <summary>
    /// Thời gian tạo record
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual OrderDetail OrderDetail { get; set; } = null!;
    public virtual MenuItem MenuItem { get; set; } = null!;
}

