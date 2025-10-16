using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs
{
    // DTO for displaying campaign data
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

        // Target KPI fields
        public int? TargetReach { get; set; }
        public decimal? TargetRevenue { get; set; }

        // Calculated fields
        public decimal? ROI => Budget.HasValue && Budget.Value > 0 && RevenueGenerated.HasValue
            ? ((RevenueGenerated.Value - Budget.Value) / Budget.Value) * 100
            : null;

        public decimal? ConversionRate => ViewCount.HasValue && ViewCount.Value > 0 && RevenueGenerated.HasValue
            ? (RevenueGenerated.Value / ViewCount.Value) * 100
            : null;

        // Target achievement percentages
        public decimal? ReachAchievementPercentage => TargetReach.HasValue && TargetReach.Value > 0
            ? ((decimal)(ViewCount ?? 0) / TargetReach.Value) * 100
            : null;

        public decimal? RevenueAchievementPercentage => TargetRevenue.HasValue && TargetRevenue.Value > 0
            ? ((RevenueGenerated ?? 0) / TargetRevenue.Value) * 100
            : null;
    }

    // DTO for creating new campaign
    public class MarketingCampaignCreateDto
    {
        [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        public string Title { get; set; } = null!;

        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        public string? Status { get; set; } = "Pending";
        public int? VoucherId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Ngân sách phải lớn hơn 0")]
        public decimal? Budget { get; set; }

        public string? CampaignType { get; set; }
        public string? TargetAudience { get; set; }

        // Target KPI
        public int? TargetReach { get; set; }
        public decimal? TargetRevenue { get; set; }
    }

    // DTO for updating campaign
    public class MarketingCampaignUpdateDto
    {
        public string? Title { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Status { get; set; }
        public int? VoucherId { get; set; }
        public decimal? Budget { get; set; }
        public string? CampaignType { get; set; }
        public string? TargetAudience { get; set; }
        public int? ViewCount { get; set; }
        public decimal? RevenueGenerated { get; set; }

        // Target KPI
        public int? TargetReach { get; set; }
        public decimal? TargetRevenue { get; set; }
    }

    // DTO for KPI metrics
    public class CampaignKpiDto
    {
        public int TotalCampaigns { get; set; }
        public decimal TotalBudgetSpent { get; set; }
        public decimal TotalRevenueGenerated { get; set; }
        public decimal AvgConversionRate { get; set; }
        public decimal TotalROI { get; set; }
    }

    // DTO for daily performance chart with KPI
    public class DailyPerformanceDto
    {
        public string Date { get; set; } = null!; // Format: "dd/MM/yyyy"
        public decimal RevenueAchievementPercent { get; set; } // % đạt KPI doanh thu
        public decimal ReachAchievementPercent { get; set; } // % đạt KPI lượt tiếp cận
        public decimal ActualRevenue { get; set; }
        public int ActualReach { get; set; }
        public decimal TargetRevenue { get; set; }
        public int TargetReach { get; set; }
    }

    // DTO for distribution chart
    public class CampaignDistributionDto
    {
        public string Type { get; set; } = null!;
        public int Count { get; set; }
    }

    // Paged Result for listing
    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}