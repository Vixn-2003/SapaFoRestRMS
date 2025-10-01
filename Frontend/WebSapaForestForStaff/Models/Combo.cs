using System;
using System.Collections.Generic;

namespace WebSapaForestForStaff.Models;

public partial class Combo
{
    public int ComboId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool? IsAvailable { get; set; }

    public string? LinkImg { get; set; }

    public virtual ICollection<ComboItem> ComboItems { get; set; } = new List<ComboItem>();
}
