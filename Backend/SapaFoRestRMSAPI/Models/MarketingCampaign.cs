using System;
using System.Collections.Generic;

namespace SapaFoRestRMSAPI.Models;

public partial class MarketingCampaign
{
    public int CampaignId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Status { get; set; }

    public int? CreatedBy { get; set; }

    public virtual User? CreatedByNavigation { get; set; }
}
