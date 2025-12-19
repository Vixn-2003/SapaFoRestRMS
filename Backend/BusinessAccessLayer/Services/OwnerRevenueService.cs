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

            // Get filtered transactions directly from database (optimized)
            var filteredTransactions = await _unitOfWork.Payments.GetFilteredTransactionsAsync(
                startDate, endDate, request.PaymentMethod, request.BranchName);

            // Convert to list for processing
            var transactionsList = filteredTransactions.ToList();

            // Get orders only for the filtered transactions to build details
            var orderIds = transactionsList.Select(t => t.OrderId).Distinct().ToList();
            var orders = await _unitOfWork.Orders.GetAllAsync();
            var relevantOrders = orders.Where(o => orderIds.Contains(o.OrderId)).ToList();

            // Build response using optimized filtered data
            var response = new RevenueResponseDto
            {
                Summary = BuildSummary(transactionsList),
                Details = BuildDetails(transactionsList, relevantOrders),
                TrendData = BuildTrendData(transactionsList),
                PaymentBreakdown = BuildPaymentBreakdown(transactionsList),
                BranchComparison = await BuildBranchComparisonAsync(transactionsList)
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
                .Where(t => t.PaymentMethod.Equals("QRBankTransfer", StringComparison.OrdinalIgnoreCase) ||
                           t.PaymentMethod.Equals("QR", StringComparison.OrdinalIgnoreCase) ||
                           t.PaymentMethod.Equals("VietQR", StringComparison.OrdinalIgnoreCase))
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
                .Where(t => t.PaymentMethod.Equals("QRBankTransfer", StringComparison.OrdinalIgnoreCase) ||
                           t.PaymentMethod.Equals("QR", StringComparison.OrdinalIgnoreCase) ||
                           t.PaymentMethod.Equals("VietQR", StringComparison.OrdinalIgnoreCase))
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

