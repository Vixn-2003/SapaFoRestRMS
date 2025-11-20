using System;

namespace DomainAccessLayer.Models;

public partial class ShiftHistory
{
    public int ShiftHistoryId { get; set; }

    public int ShiftId { get; set; }

    public int ActionBy { get; set; }   // UserId

    public string Action { get; set; } = null!;
    // Create / Update / Cancel / Move / Assign

    public DateTime ActionAt { get; set; }

    public string? Detail { get; set; }

    public virtual Shift Shift { get; set; } = null!;
}
