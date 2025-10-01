using System;
using System.Collections.Generic;

namespace SapaFoRestRMSAPI.Models;

public partial class SalaryRule
{
    public int RuleId { get; set; }

    public int? BaseWorkDays { get; set; }

    public int? FullSalaryCondition { get; set; }

    public decimal? BonusPerShift { get; set; }

    public decimal? PenaltyLate { get; set; }

    public decimal? PenaltyAbsent { get; set; }
}
