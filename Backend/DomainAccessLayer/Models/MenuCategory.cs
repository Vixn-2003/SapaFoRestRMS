using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class MenuCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
