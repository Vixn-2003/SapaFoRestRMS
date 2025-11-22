using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class OrderDetail
{
    public int OrderDetailId { get; set; }

    public int OrderId { get; set; }

    public int? MenuItemId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public string? Status { get; set; }

    //new
    public DateTime CreatedAt { get; set; }

    // === THÊM CÁC DÒNG NÀY ===
    // Thêm cột ComboId (nullable)
    public int? ComboId { get; set; }
    public Combo Combo { get; set; }

    public string? Notes { get; set; } // Thêm ? để cho phép null
    
    /// <summary>
    /// Đánh dấu order được yêu cầu làm ngay từ bếp phó
    /// </summary>
    public bool IsUrgent { get; set; } = false;

    /// <summary>
    /// Thời gian món được đánh dấu "Sẵn sàng" (Ready)
    /// </summary>
    public DateTime? ReadyAt { get; set; }

    /// <summary>
    /// Thời gian bắt đầu nấu (khi status chuyển sang "Cooking")
    /// </summary>
    public DateTime? StartedAt { get; set; }

    public virtual ICollection<KitchenTicketDetail> KitchenTicketDetails { get; set; } = new List<KitchenTicketDetail>();

    public virtual MenuItem MenuItem { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
