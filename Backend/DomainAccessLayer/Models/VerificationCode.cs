using System;

namespace DomainAccessLayer.Models;

public partial class VerificationCode
{
    public int VerificationCodeId { get; set; }

    public int UserId { get; set; }

    public string Code { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public string Purpose { get; set; } = null!; // e.g., "ResetPassword", "ChangePassword"

    public virtual User User { get; set; } = null!;
}


