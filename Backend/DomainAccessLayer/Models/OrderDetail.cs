using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomainAccessLayer.Models;

public partial class OrderDetail
{
    public int OrderDetailId { get; set; }

    public int OrderId { get; set; }

    public int? MenuItemId { get; set; }

    public int Quantity { get; set; }

    /// <summary>
    /// Số lượng thực tế khách sử dụng (dành cho ConsumptionBased items)
    /// Null = chưa được xác nhận bởi khách
    /// Nếu MenuItem.BillingType = ConsumptionBased thì tính tiền theo QuantityUsed
    /// Nếu MenuItem.BillingType = KitchenPrepared thì tính tiền theo Quantity (100%)
    /// </summary>
    public int? QuantityUsed { get; set; }

    public decimal UnitPrice { get; set; }

    public string? Status { get; set; }

    //new
    public DateTime CreatedAt { get; set; }

    // === THÊM CÁC DÒNG NÀY ===
    // Thêm cột ComboId (nullable)
    public int? ComboId { get; set; }
    public Combo? Combo { get; set; }

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

    [NotMapped] // Nếu không muốn lưu vào DB
    public bool IsCustomerOrder { get; set; }

    public virtual ICollection<KitchenTicketDetail> KitchenTicketDetails { get; set; } = new List<KitchenTicketDetail>();

    /// <summary>
    /// Các món con trong combo (nếu OrderDetail này là một Combo)
    /// </summary>
    public virtual ICollection<OrderComboItem> OrderComboItems { get; set; } = new List<OrderComboItem>();

    public virtual MenuItem MenuItem { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
