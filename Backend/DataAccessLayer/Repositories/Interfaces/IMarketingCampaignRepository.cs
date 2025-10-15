using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IMarketingCampaignRepository
    {
        Task<IEnumerable<MarketingCampaign>> GetAllAsync();
        Task<MarketingCampaign?> GetByIdAsync(int id);
        Task<MarketingCampaign> AddAsync(MarketingCampaign campaign);
        Task UpdateAsync(MarketingCampaign campaign);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);

        // Advanced queries
        Task<(IEnumerable<MarketingCampaign> data, int totalCount)> SearchFilterPaginateAsync(
            string? searchTerm,
            string? campaignType,
            string? status,
            DateOnly? startDate,
            DateOnly? endDate,
            int pageNumber,
            int pageSize);

        // KPI queries
        Task<int> GetTotalCampaignsAsync(DateOnly? startDate, DateOnly? endDate);
        Task<decimal> GetTotalBudgetSpentAsync(DateOnly? startDate, DateOnly? endDate);
        Task<decimal> GetTotalRevenueGeneratedAsync(DateOnly? startDate, DateOnly? endDate);
        Task<decimal> GetAverageConversionRateAsync(DateOnly? startDate, DateOnly? endDate);
        Task<decimal> GetTotalROIAsync(DateOnly? startDate, DateOnly? endDate);

        // Chart data
        Task<IEnumerable<(string Month, decimal Revenue, int Reach, decimal Budget)>> GetPerformanceDataAsync(
            DateOnly startDate, DateOnly endDate);

        Task<IEnumerable<(string Month, decimal Revenue, int Reach, decimal Budget)>> GetPerformanceDataForPreviousYearAsync(
            DateOnly startDate, DateOnly endDate);

        Task<IEnumerable<(string Type, int Count)>> GetCampaignDistributionAsync();

        // Target KPI
        Task<(decimal revenue, int reach)> GetCurrentPeriodMetricsAsync(DateOnly startDate, DateOnly endDate);
    }
}