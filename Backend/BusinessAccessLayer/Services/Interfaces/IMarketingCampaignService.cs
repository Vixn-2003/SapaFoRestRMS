using BusinessAccessLayer.DTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IMarketingCampaignService
    {
        Task<IEnumerable<MarketingCampaignDto>> GetAllAsync();
        Task<MarketingCampaignDto?> GetByIdAsync(int id);
        Task<MarketingCampaignDto> CreateAsync(MarketingCampaignCreateDto dto, IFormFile? imageFile);
        Task<MarketingCampaignDto?> UpdateAsync(int id, MarketingCampaignUpdateDto dto, IFormFile? imageFile);
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Search, filter and paginate campaigns
        /// </summary>
        Task<(IEnumerable<MarketingCampaignDto> data, int totalCount)> SearchFilterPaginateAsync(
            string? searchTerm,
            string? campaignType,
            string? status,
            DateOnly? startDate,
            DateOnly? endDate,
            int pageNumber,
            int pageSize);

        /// <summary>
        /// Get KPI metrics for dashboard
        /// </summary>
        Task<CampaignKpiDto> GetKpisAsync(DateOnly? startDate, DateOnly? endDate);

        /// <summary>
        /// Get performance data for line chart
        /// </summary>
        Task<IEnumerable<CampaignPerformanceDto>> GetPerformanceChartDataAsync(
            DateOnly startDate, DateOnly endDate);

        /// <summary>
        /// Get comparison with previous year
        /// </summary>
        Task<CampaignComparisonDto> GetYearOverYearComparisonAsync(
            DateOnly startDate, DateOnly endDate);

        /// <summary>
        /// Get target KPI achievement
        /// </summary>
        Task<CampaignTargetKpiDto> GetTargetKpiAsync(
            DateOnly startDate,
            DateOnly endDate,
            decimal targetRevenue,
            int targetReach);

        /// <summary>
        /// Get campaign distribution by type (optional)
        /// </summary>
        Task<IEnumerable<CampaignDistributionDto>> GetCampaignDistributionAsync();
    }
}