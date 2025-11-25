using System;

namespace WebSapaForestForStaff.Models;

public partial class OrderHistory
{
    public int OrderHistoryId { get; set; }

    public int OrderId { get; set; }

    public string Action { get; set; } = null!;

    public string? Reason { get; set; }

    public int StaffId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Staff? Staff { get; set; }
}

