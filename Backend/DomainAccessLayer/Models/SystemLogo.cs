using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class SystemLogo
{
    public int LogoId { get; set; }

    public string LogoName { get; set; } = null!;

    public string LogoUrl { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public int? CreatedBy { get; set; }

    public virtual User? CreatedByNavigation { get; set; }
}
