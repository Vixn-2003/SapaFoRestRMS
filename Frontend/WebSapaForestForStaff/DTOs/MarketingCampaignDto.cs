using System;
using System.ComponentModel.DataAnnotations;

namespace WebSapaFoRestRMSForStaff.Models.CampaignDTO
{
    public class MarketingCampaignDto
    {
        public int CampaignId { get; set; }
        public string Title { get; set; } = null!;
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
        public int? TargetReach { get; set; }
        public decimal? TargetRevenue { get; set; }

        // Calculated properties
        public decimal? ROI => Budget.HasValue && Budget.Value > 0 && RevenueGenerated.HasValue
            ? ((RevenueGenerated.Value - Budget.Value) / Budget.Value) * 100
            : null;
    }

    public class MarketingCampaignCreateDto
    {
        [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
        public string Title { get; set; } = null!;

        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Status { get; set; } = "Pending";
        public int? VoucherId { get; set; }
        public decimal? Budget { get; set; }
        public string? CampaignType { get; set; }
        public string? TargetAudience { get; set; }
        public int? TargetReach { get; set; }
        public decimal? TargetRevenue { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}