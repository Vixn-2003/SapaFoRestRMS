using System;
using System.Collections.Generic;
using DomainAccessLayer.Enums;

namespace DomainAccessLayer.Models;

public partial class MenuItem
{
    public int MenuItemId { get; set; }

    public int? CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string CourseType { get; set; } = null!;

    public bool? IsAvailable { get; set; }
    public bool? IsAds { get; set; }

    public string? ImageUrl { get; set; }

    public int? TimeCook { get; set; } // Thời gian nấu (phút)

    public int? BatchSize { get; set; } // Số lượng mỗi mẻ nấu

    /// <summary>
    /// Loại hình tính tiền: ConsumptionBased (theo SL dùng) hoặc KitchenPrepared (theo SL đặt)
    /// Mặc định: KitchenPrepared (giữ nguyên logic cũ cho món đã có)
    /// </summary>
    public ItemBillingType BillingType { get; set; } = ItemBillingType.KitchenPrepared;

    public virtual MenuCategory? Category { get; set; }

    public virtual ICollection<ComboItem> ComboItems { get; set; } = new List<ComboItem>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}
