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
        /// Get daily performance data with KPI achievement percentages
        /// </summary>
        Task<IEnumerable<DailyPerformanceDto>> GetDailyPerformanceChartDataAsync(
            DateOnly startDate, DateOnly endDate);

        /// <summary>
        /// Get daily performance for previous year (same period)
        /// </summary>
        Task<IEnumerable<DailyPerformanceDto>> GetDailyPerformancePreviousYearAsync(
            DateOnly startDate, DateOnly endDate);


    }
}