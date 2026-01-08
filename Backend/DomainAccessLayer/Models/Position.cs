using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class Position
{
    public int PositionId { get; set; }

    public string PositionName { get; set; } = null!;

    public string? Description { get; set; }

    public int Status { get; set; }

    /// <summary>
    /// Lương cơ bản cho position này (VND)
    /// </summary>
    public decimal BaseSalary { get; set; } = 0;

    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();

    public virtual ICollection<SalaryChangeRequest> SalaryChangeRequests { get; set; } = new List<SalaryChangeRequest>();
}


