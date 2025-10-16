using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class MarketingCampaignRepository : IMarketingCampaignRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public MarketingCampaignRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MarketingCampaign>> GetAllAsync()
        {
            return await _context.MarketingCampaigns
                .Include(c => c.Voucher)
                .Include(c => c.CreatedByNavigation)
                .ToListAsync();
        }

        public async Task<MarketingCampaign?> GetByIdAsync(int id)
        {
            return await _context.MarketingCampaigns
                .Include(c => c.Voucher)
                .Include(c => c.CreatedByNavigation)
                .FirstOrDefaultAsync(c => c.CampaignId == id);
        }

        public async Task<MarketingCampaign> AddAsync(MarketingCampaign campaign)
        {
            _context.MarketingCampaigns.Add(campaign);
            await _context.SaveChangesAsync();
            return campaign;
        }

        public async Task UpdateAsync(MarketingCampaign campaign)
        {
            _context.Entry(campaign).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var campaign = await _context.MarketingCampaigns.FindAsync(id);
            if (campaign != null)
            {
                _context.MarketingCampaigns.Remove(campaign);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.MarketingCampaigns.AnyAsync(c => c.CampaignId == id);
        }

        public async Task<(IEnumerable<MarketingCampaign> data, int totalCount)> SearchFilterPaginateAsync(
            string? searchTerm,
            string? campaignType,
            string? status,
            DateOnly? startDate,
            DateOnly? endDate,
            int pageNumber,
            int pageSize)
        {
            var query = _context.MarketingCampaigns
                .Include(c => c.Voucher)
                .Include(c => c.CreatedByNavigation)
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(c =>
                    c.Title.ToLower().Contains(term) ||
                    (c.TargetAudience != null && c.TargetAudience.ToLower().Contains(term)));
            }

            // Filter by campaign type
            if (!string.IsNullOrWhiteSpace(campaignType))
            {
                query = query.Where(c => c.CampaignType == campaignType);
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status == status);
            }

            // Filter by date range
            if (startDate.HasValue)
            {
                query = query.Where(c => c.StartDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(c => c.EndDate <= endDate.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Pagination
            var data = await query
                .OrderByDescending(c => c.StartDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, totalCount);
        }

        public async Task<int> GetTotalCampaignsAsync(DateOnly? startDate, DateOnly? endDate)
        {
            var query = _context.MarketingCampaigns.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(c => c.StartDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(c => c.EndDate <= endDate.Value);

            return await query.CountAsync();
        }

        public async Task<decimal> GetTotalBudgetSpentAsync(DateOnly? startDate, DateOnly? endDate)
        {
            var query = _context.MarketingCampaigns.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(c => c.StartDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(c => c.EndDate <= endDate.Value);

            return await query.SumAsync(c => c.Budget ?? 0);
        }

        public async Task<decimal> GetTotalRevenueGeneratedAsync(DateOnly? startDate, DateOnly? endDate)
        {
            var query = _context.MarketingCampaigns.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(c => c.StartDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(c => c.EndDate <= endDate.Value);

            return await query.SumAsync(c => c.RevenueGenerated ?? 0);
        }

        public async Task<decimal> GetAverageConversionRateAsync(DateOnly? startDate, DateOnly? endDate)
        {
            var query = _context.MarketingCampaigns
                .Where(c => c.ViewCount.HasValue && c.ViewCount.Value > 0)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(c => c.StartDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(c => c.EndDate <= endDate.Value);

            var campaigns = await query.ToListAsync();

            if (!campaigns.Any())
                return 0;

            var totalConversionRate = campaigns
                .Where(c => c.RevenueGenerated.HasValue)
                .Sum(c => (c.RevenueGenerated.Value / c.ViewCount!.Value) * 100);

            return totalConversionRate / campaigns.Count;
        }

        public async Task<decimal> GetTotalROIAsync(DateOnly? startDate, DateOnly? endDate)
        {
            var query = _context.MarketingCampaigns
                .Where(c => c.Budget.HasValue && c.Budget.Value > 0 && c.RevenueGenerated.HasValue)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(c => c.StartDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(c => c.EndDate <= endDate.Value);

            var campaigns = await query.ToListAsync();

            if (!campaigns.Any())
                return 0;

            var totalBudget = campaigns.Sum(c => c.Budget!.Value);
            var totalRevenue = campaigns.Sum(c => c.RevenueGenerated!.Value);

            return totalBudget > 0 ? ((totalRevenue - totalBudget) / totalBudget) * 100 : 0;
        }

        public async Task<IEnumerable<(DateOnly Date, decimal Revenue, int Reach, decimal TargetRevenue, int TargetReach)>> GetDailyPerformanceDataAsync(
            DateOnly startDate, DateOnly endDate)
        {
            var campaigns = await _context.MarketingCampaigns
                .Where(c => c.StartDate >= startDate && c.EndDate <= endDate)
                .ToListAsync();

            var dailyData = new Dictionary<DateOnly, (decimal Revenue, int Reach, decimal TargetRevenue, int TargetReach)>();

            foreach (var campaign in campaigns)
            {
                if (!campaign.StartDate.HasValue || !campaign.EndDate.HasValue)
                    continue;

                var campaignStart = campaign.StartDate.Value;
                var campaignEnd = campaign.EndDate.Value;
                var daysInCampaign = (campaignEnd.ToDateTime(TimeOnly.MinValue) - campaignStart.ToDateTime(TimeOnly.MinValue)).Days + 1;

                if (daysInCampaign <= 0) daysInCampaign = 1;

                // Distribute metrics evenly across campaign days
                var dailyRevenue = (campaign.RevenueGenerated ?? 0) / daysInCampaign;
                var dailyReach = (campaign.ViewCount ?? 0) / daysInCampaign;
                var dailyTargetRevenue = (campaign.TargetRevenue ?? 0) / daysInCampaign;
                var dailyTargetReach = (campaign.TargetReach ?? 0) / daysInCampaign;

                for (var date = campaignStart; date <= campaignEnd; date = date.AddDays(1))
                {
                    if (date < startDate || date > endDate) continue;

                    if (!dailyData.ContainsKey(date))
                    {
                        dailyData[date] = (0, 0, 0, 0);
                    }

                    var existing = dailyData[date];
                    dailyData[date] = (
                        existing.Revenue + dailyRevenue,
                        existing.Reach + dailyReach,
                        existing.TargetRevenue + dailyTargetRevenue,
                        existing.TargetReach + dailyTargetReach
                    );
                }
            }

            return dailyData
                .OrderBy(x => x.Key)
                .Select(x => (x.Key, x.Value.Revenue, x.Value.Reach, x.Value.TargetRevenue, x.Value.TargetReach));
        }

        public async Task<IEnumerable<(DateOnly Date, decimal Revenue, int Reach, decimal TargetRevenue, int TargetReach)>> GetDailyPerformanceDataForPreviousYearAsync(
            DateOnly startDate, DateOnly endDate)
        {
            var previousYearStart = startDate.AddYears(-1);
            var previousYearEnd = endDate.AddYears(-1);

            return await GetDailyPerformanceDataAsync(previousYearStart, previousYearEnd);
        }


    }
}