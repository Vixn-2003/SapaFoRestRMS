using BusinessAccessLayer.DTOs.Owner;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    /// <summary>
    /// Service xử lý business logic cho Owner Dashboard
    /// </summary>
    public class OwnerDashboardService : IOwnerDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OwnerDashboardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<OwnerDashboardDto> GetDashboardDataAsync(CancellationToken ct = default)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var startOfMonth = new DateOnly(today.Year, today.Month, 1);
            var yesterday = today.AddDays(-1);
            var lastMonth = startOfMonth.AddMonths(-1);

            // Load all data first (materialize) to avoid DbContext threading issues
            // Then process in parallel on in-memory data
            var orders = (await _unitOfWork.Orders.GetAllAsync()).ToList();
            var transactions = (await _unitOfWork.Payments.GetAllTransactionsAsync()).ToList();
            var orderDetails = (await _unitOfWork.OrderDetails.GetAllAsync()).ToList();
            var ingredients = (await _unitOfWork.InventoryIngredient.GetAllAsync()).ToList();

            // Now process in parallel on in-memory data
            var kpiTask = Task.Run(() => GetKpiCardsAsync(today, startOfMonth, yesterday, lastMonth, orders, transactions, ingredients));
            var revenueTrendTask = Task.Run(() => GetRevenueTrendAsync(today.AddDays(-30), today, transactions));
            var topSellingTask = Task.Run(() => GetTopSellingItemsAsync(startOfMonth, today, orders, orderDetails));
            var branchComparisonTask = Task.Run(() => GetBranchComparisonAsync(startOfMonth, today, transactions));
            var alertsTask = Task.Run(() => GetAlertsSummaryAsync(today, ingredients));

            await Task.WhenAll(kpiTask, revenueTrendTask, topSellingTask, branchComparisonTask, alertsTask);

            return new OwnerDashboardDto
            {
                KpiCards = await kpiTask,
                RevenueTrend = await revenueTrendTask,
                TopSellingItems = await topSellingTask,
                BranchComparison = await branchComparisonTask,
                AlertsSummary = await alertsTask
            };
        }

        private KpiCardsDto GetKpiCardsAsync(DateOnly today, DateOnly startOfMonth, DateOnly yesterday, DateOnly lastMonth, 
            List<Order> orders, 
            List<Transaction> transactions,
            List<Ingredient> ingredients)
        {

            // Today Revenue
            var todayRevenue = transactions
                .Where(t => t.Status == "Paid" && (t.CompletedAt.HasValue || t.CreatedAt != default))
                .Where(t => DateOnly.FromDateTime(t.CompletedAt ?? t.CreatedAt) == today)
                .Sum(t => t.Amount);

            // Yesterday Revenue (for comparison)
            var yesterdayRevenue = transactions
                .Where(t => t.Status == "Paid" && (t.CompletedAt.HasValue || t.CreatedAt != default))
                .Where(t => DateOnly.FromDateTime(t.CompletedAt ?? t.CreatedAt) == yesterday)
                .Sum(t => t.Amount);

            // Monthly Revenue
            var monthlyRevenue = transactions
                .Where(t => t.Status == "Paid" && (t.CompletedAt.HasValue || t.CreatedAt != default))
                .Where(t => DateOnly.FromDateTime(t.CompletedAt ?? t.CreatedAt) >= startOfMonth)
                .Sum(t => t.Amount);

            // Last Month Revenue (for comparison)
            var lastMonthRevenue = transactions
                .Where(t => t.Status == "Paid" && (t.CompletedAt.HasValue || t.CreatedAt != default))
                .Where(t => {
                    var date = DateOnly.FromDateTime(t.CompletedAt ?? t.CreatedAt);
                    return date >= lastMonth && date < startOfMonth;
                })
                .Sum(t => t.Amount);

            // Total Orders (this month)
            var totalOrders = orders
                .Where(o => o.Status == "Paid" && 
                       o.CreatedAt.HasValue && 
                       DateOnly.FromDateTime(o.CreatedAt.Value) >= startOfMonth)
                .Count();

            // Active Customers (customers with orders this month)
            var activeCustomers = orders
                .Where(o => o.Status == "Paid" && 
                       o.CreatedAt.HasValue && 
                       DateOnly.FromDateTime(o.CreatedAt.Value) >= startOfMonth &&
                       o.CustomerId.HasValue)
                .Select(o => o.CustomerId.Value)
                .Distinct()
                .Count();

            // Low Stock Alerts
            var lowStockCount = ingredients.Count(i => 
                i.ReorderLevel.HasValue && 
                i.InventoryBatches.Sum(b => b.Available) < i.ReorderLevel.Value);

            // Near Expiry Alerts (within 7 days)
            var nearExpiryCount = ingredients
                .SelectMany(i => i.InventoryBatches)
                .Count(b => b.ExpiryDate.HasValue && 
                       b.ExpiryDate.Value <= today.AddDays(7) &&
                       b.ExpiryDate.Value > today &&
                       b.IsActive);

            // Calculate change percentages
            var todayChangePercent = yesterdayRevenue > 0 
                ? ((todayRevenue - yesterdayRevenue) / yesterdayRevenue) * 100 
                : 0;

            var monthlyChangePercent = lastMonthRevenue > 0 
                ? ((monthlyRevenue - lastMonthRevenue) / lastMonthRevenue) * 100 
                : 0;

            return new KpiCardsDto
            {
                TodayRevenue = todayRevenue,
                MonthlyRevenue = monthlyRevenue,
                TotalOrders = totalOrders,
                ActiveCustomers = activeCustomers,
                LowStockAlertsCount = lowStockCount,
                NearExpiryAlertsCount = nearExpiryCount,
                TodayRevenueChangePercent = todayChangePercent,
                MonthlyRevenueChangePercent = monthlyChangePercent
            };
        }

        private List<RevenueTrendDataDto> GetRevenueTrendAsync(DateOnly startDate, DateOnly endDate, 
            List<Transaction> transactions)
        {

            var trendData = transactions
                .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
                .GroupBy(t => DateOnly.FromDateTime(t.CompletedAt.Value))
                .Where(g => g.Key >= startDate && g.Key <= endDate)
                .Select(g => new RevenueTrendDataDto
                {
                    Date = g.Key.ToString("dd/MM"),
                    Revenue = g.Sum(t => t.Amount),
                    OrderCount = g.Select(t => t.OrderId).Distinct().Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            return trendData;
        }

        private List<TopSellingItemDto> GetTopSellingItemsAsync(DateOnly startDate, DateOnly endDate,
            List<Order> orders,
            List<OrderDetail> orderDetails)
        {

            var paidOrderIds = orders
                .Where(o => o.Status == "Paid" && 
                       o.CreatedAt.HasValue && 
                       DateOnly.FromDateTime(o.CreatedAt.Value) >= startDate &&
                       DateOnly.FromDateTime(o.CreatedAt.Value) <= endDate)
                .Select(o => o.OrderId)
                .ToHashSet();

            var topItems = orderDetails
                .Where(od => paidOrderIds.Contains(od.OrderId) && od.MenuItemId.HasValue && od.MenuItem != null)
                .GroupBy(od => new { od.MenuItemId, od.MenuItem!.Name })
                .Select(g => new TopSellingItemDto
                {
                    ItemName = g.Key.Name,
                    QuantitySold = g.Sum(od => od.QuantityUsed ?? od.Quantity),
                    Revenue = g.Sum(od => (od.QuantityUsed ?? od.Quantity) * od.UnitPrice)
                })
                .OrderByDescending(i => i.QuantitySold)
                .Take(10)
                .ToList();

            return topItems;
        }

        private List<BranchComparisonDto> GetBranchComparisonAsync(DateOnly startDate, DateOnly endDate,
            List<Transaction> transactions)
        {
            // Hiện tại chỉ có 1 branch, trả về data mẫu
            // TODO: Implement khi có multi-branch

            var totalRevenue = transactions
                .Where(t => t.Status == "Paid" && 
                       t.CompletedAt.HasValue &&
                       DateOnly.FromDateTime(t.CompletedAt.Value) >= startDate &&
                       DateOnly.FromDateTime(t.CompletedAt.Value) <= endDate)
                .Sum(t => t.Amount);

            var totalOrders = transactions
                .Where(t => t.Status == "Paid" && 
                       t.CompletedAt.HasValue &&
                       DateOnly.FromDateTime(t.CompletedAt.Value) >= startDate &&
                       DateOnly.FromDateTime(t.CompletedAt.Value) <= endDate)
                .Select(t => t.OrderId)
                .Distinct()
                .Count();

            return new List<BranchComparisonDto>
            {
                new BranchComparisonDto
                {
                    BranchName = "Sapa Forest Restaurant",
                    Revenue = totalRevenue,
                    OrderCount = totalOrders
                }
            };
        }

        private AlertsSummaryDto GetAlertsSummaryAsync(DateOnly today,
            List<Ingredient> ingredients)
        {

            // Low Stock Count
            var lowStockCount = ingredients.Count(i => 
                i.ReorderLevel.HasValue && 
                i.InventoryBatches.Sum(b => b.Available) < i.ReorderLevel.Value);

            // Near Expiry Count (within 7 days)
            var nearExpiryCount = ingredients
                .SelectMany(i => i.InventoryBatches)
                .Count(b => b.ExpiryDate.HasValue && 
                       b.ExpiryDate.Value <= today.AddDays(7) &&
                       b.ExpiryDate.Value > today &&
                       b.IsActive);

            // Expired Count
            var expiredCount = ingredients
                .SelectMany(i => i.InventoryBatches)
                .Count(b => b.ExpiryDate.HasValue && 
                       b.ExpiryDate.Value <= today &&
                       b.IsActive);

            return new AlertsSummaryDto
            {
                LowStockCount = lowStockCount,
                NearExpiryCount = nearExpiryCount,
                ExpiredCount = expiredCount
            };
        }
    }
}

