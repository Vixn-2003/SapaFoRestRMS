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

        // Chart data - Daily performance with KPI targets
        Task<IEnumerable<(DateOnly Date, decimal Revenue, int Reach, decimal TargetRevenue, int TargetReach)>> GetDailyPerformanceDataAsync(
            DateOnly startDate, DateOnly endDate);

        Task<IEnumerable<(DateOnly Date, decimal Revenue, int Reach, decimal TargetRevenue, int TargetReach)>> GetDailyPerformanceDataForPreviousYearAsync(
            DateOnly startDate, DateOnly endDate);


    }
}