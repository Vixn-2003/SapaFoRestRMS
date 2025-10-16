using System;
using System.Collections.Generic;

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

    public string? ImageUrl { get; set; }

    public virtual MenuCategory? Category { get; set; }

    public virtual ICollection<ComboItem> ComboItems { get; set; } = new List<ComboItem>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
}
