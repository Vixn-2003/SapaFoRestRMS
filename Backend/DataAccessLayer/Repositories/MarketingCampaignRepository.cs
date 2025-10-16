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

        public async Task<IEnumerable<(string Month, decimal Revenue, int Reach, decimal Budget)>> GetPerformanceDataAsync(
            DateOnly startDate, DateOnly endDate)
        {
            var campaigns = await _context.MarketingCampaigns
                .Where(c => c.StartDate >= startDate && c.EndDate <= endDate)
                .ToListAsync();

            var grouped = campaigns
                .GroupBy(c => new { Year = c.StartDate!.Value.Year, Month = c.StartDate!.Value.Month })
                .Select(g => new
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Revenue = g.Sum(c => c.RevenueGenerated ?? 0),
                    Reach = g.Sum(c => c.ViewCount ?? 0),
                    Budget = g.Sum(c => c.Budget ?? 0)
                })
                .OrderBy(x => x.Month)
                .ToList();

            return grouped.Select(g => (g.Month, g.Revenue, g.Reach, g.Budget));
        }

        public async Task<IEnumerable<(string Month, decimal Revenue, int Reach, decimal Budget)>> GetPerformanceDataForPreviousYearAsync(
            DateOnly startDate, DateOnly endDate)
        {
            var previousYearStart = startDate.AddYears(-1);
            var previousYearEnd = endDate.AddYears(-1);

            return await GetPerformanceDataAsync(previousYearStart, previousYearEnd);
        }

        public async Task<IEnumerable<(string Type, int Count)>> GetCampaignDistributionAsync()
        {
            var distribution = await _context.MarketingCampaigns
                .GroupBy(c => c.CampaignType ?? "Unknown")
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            return distribution.Select(d => (d.Type, d.Count));
        }

        public async Task<(decimal revenue, int reach)> GetCurrentPeriodMetricsAsync(DateOnly startDate, DateOnly endDate)
        {
            var campaigns = await _context.MarketingCampaigns
                .Where(c => c.StartDate >= startDate && c.EndDate <= endDate)
                .ToListAsync();

            var revenue = campaigns.Sum(c => c.RevenueGenerated ?? 0);
            var reach = campaigns.Sum(c => c.ViewCount ?? 0);

            return (revenue, reach);
        }
    }
}