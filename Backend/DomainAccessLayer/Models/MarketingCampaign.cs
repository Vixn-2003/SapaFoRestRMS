using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models
{
    public partial class MarketingCampaign
    {
        public int CampaignId { get; set; }
        public string Title { get; set; } = null!;
        // REMOVED: public string? Description { get; set; } 
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Status { get; set; }
        public int? CreatedBy { get; set; }
        public int? VoucherId { get; set; }
        public decimal? Budget { get; set; }
        public string? CampaignType { get; set; }
        public string? TargetAudience { get; set; }
        public string? ImageUrl { get; set; }
        public int? ViewCount { get; set; }
        public decimal? RevenueGenerated { get; set; }

        // NEW: Target KPI fields
        public int? TargetReach { get; set; }
        public decimal? TargetRevenue { get; set; }

        // Navigation properties
        public virtual User? CreatedByNavigation { get; set; }
        public virtual Voucher? Voucher { get; set; }
    }
}