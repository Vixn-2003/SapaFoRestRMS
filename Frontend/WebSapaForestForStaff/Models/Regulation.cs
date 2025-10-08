using System;
using System.Collections.Generic;

namespace WebSapaForestForStaff.Models;

public partial class Regulation
{
    public int RegulationId { get; set; }

    public string Title { get; set; } = null!;

    public string? Content { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public virtual User? CreatedByNavigation { get; set; }
}
