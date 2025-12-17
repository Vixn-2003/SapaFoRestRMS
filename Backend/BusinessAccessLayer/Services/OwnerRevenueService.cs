using BusinessAccessLayer.DTOs.Owner;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    /// <summary>
    /// Service xử lý business logic cho Owner Revenue Management
    /// </summary>
    public class OwnerRevenueService : IOwnerRevenueService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OwnerRevenueService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<RevenueResponseDto> GetRevenueDataAsync(RevenueFilterRequestDto request, CancellationToken ct = default)
        {
            // Set default date range if not provided
            var endDate = request.EndDate ?? DateTime.Today;
            var startDate = request.StartDate ?? endDate.AddDays(-30);

            var transactions = await _unitOfWork.Payments.GetAllTransactionsAsync();
            var orders = await _unitOfWork.Orders.GetAllAsync();

            // Filter transactions
            var filteredTransactions = transactions
                .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
                .Where(t => t.CompletedAt.Value.Date >= startDate.Date && t.CompletedAt.Value.Date <= endDate.Date)
                .ToList();

            // Filter by payment method if specified
            if (!string.IsNullOrEmpty(request.PaymentMethod) && request.PaymentMethod != "ALL")
            {
                filteredTransactions = filteredTransactions
                    .Where(t => t.PaymentMethod.Equals(request.PaymentMethod, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // TODO: Filter by branch when multi-branch is implemented
            // For now, ignore branch filter

            // Build response
            var response = new RevenueResponseDto
            {
                Summary = BuildSummary(filteredTransactions),
                Details = BuildDetails(filteredTransactions, orders),
                TrendData = BuildTrendData(filteredTransactions),
                PaymentBreakdown = BuildPaymentBreakdown(filteredTransactions),
                BranchComparison = await BuildBranchComparisonAsync(filteredTransactions)
            };

            return response;
        }

        private RevenueSummaryDto BuildSummary(List<DomainAccessLayer.Models.Transaction> transactions)
        {
            var totalRevenue = transactions.Sum(t => t.Amount);
            var totalOrders = transactions.Select(t => t.OrderId).Distinct().Count();
            var averagePerOrder = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            var cashRevenue = transactions
                .Where(t => t.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase))
                .Sum(t => t.Amount);

            var qrRevenue = transactions
                .Where(t => t.PaymentMethod.Equals("QR", StringComparison.OrdinalIgnoreCase))
                .Sum(t => t.Amount);

            var combinedRevenue = transactions
                .Where(t => t.PaymentMethod.Equals("Combined", StringComparison.OrdinalIgnoreCase))
                .Sum(t => t.Amount);

            return new RevenueSummaryDto
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                AveragePerOrder = averagePerOrder,
                CashRevenue = cashRevenue,
                QrRevenue = qrRevenue,
                CombinedRevenue = combinedRevenue
            };
        }

        private List<RevenueDetailDto> BuildDetails(List<DomainAccessLayer.Models.Transaction> transactions, IEnumerable<DomainAccessLayer.Models.Order> orders)
        {
            var orderDict = orders.ToDictionary(o => o.OrderId);

            return transactions
                .OrderByDescending(t => t.CompletedAt)
                .Select(t => new RevenueDetailDto
                {
                    OrderId = t.OrderId,
                    TransactionCode = t.TransactionCode,
                    Date = t.CompletedAt ?? t.CreatedAt,
                    PaymentMethod = t.PaymentMethod,
                    Amount = t.Amount,
                    Status = t.Status,
                    CustomerName = orderDict.ContainsKey(t.OrderId) && orderDict[t.OrderId].Customer != null
                        ? orderDict[t.OrderId].Customer!.User.FullName
                        : "Guest",
                    BranchName = "Sapa Forest Restaurant" // TODO: Multi-branch support
                })
                .ToList();
        }

        private List<RevenueTrendDataDto> BuildTrendData(List<DomainAccessLayer.Models.Transaction> transactions)
        {
            return transactions
                .GroupBy(t => DateOnly.FromDateTime(t.CompletedAt ?? t.CreatedAt))
                .Select(g => new RevenueTrendDataDto
                {
                    Date = g.Key.ToString("dd/MM/yyyy"),
                    Revenue = g.Sum(t => t.Amount),
                    OrderCount = g.Select(t => t.OrderId).Distinct().Count()
                })
                .OrderBy(d => d.Date)
                .ToList();
        }

        private PaymentMethodBreakdownDto BuildPaymentBreakdown(List<DomainAccessLayer.Models.Transaction> transactions)
        {
            var cashTransactions = transactions
                .Where(t => t.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var qrTransactions = transactions
                .Where(t => t.PaymentMethod.Equals("QR", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var combinedTransactions = transactions
                .Where(t => t.PaymentMethod.Equals("Combined", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return new PaymentMethodBreakdownDto
            {
                CashAmount = cashTransactions.Sum(t => t.Amount),
                QrAmount = qrTransactions.Sum(t => t.Amount),
                CombinedAmount = combinedTransactions.Sum(t => t.Amount),
                CashCount = cashTransactions.Count,
                QrCount = qrTransactions.Count,
                CombinedCount = combinedTransactions.Count
            };
        }

        private async Task<List<BranchComparisonDto>> BuildBranchComparisonAsync(List<DomainAccessLayer.Models.Transaction> transactions)
        {
            // TODO: Implement multi-branch comparison
            // For now, return single branch data
            var totalRevenue = transactions.Sum(t => t.Amount);
            var totalOrders = transactions.Select(t => t.OrderId).Distinct().Count();

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
    }
}

