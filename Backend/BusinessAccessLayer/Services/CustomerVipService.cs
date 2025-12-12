using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.Customers;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessAccessLayer.Services
{
    public class CustomerVipService : ICustomerVipService
    {
        private readonly SapaFoRestRmsContext _context;
        private readonly ILogger<CustomerVipService> _logger;
        private const string ManualOverrideToken = "[VIP_MANUAL_OVERRIDE]";

        public CustomerVipService(SapaFoRestRmsContext context, ILogger<CustomerVipService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CustomerVipStatisticsDto> CalculateVipStatusAsync(Customer customer, CancellationToken ct = default)
        {
            var stats = await BuildStatisticsAsync(customer, ct);
            // For calculation only, return the computed VIP as the recommended value.
            stats.IsVip = stats.ComputedVip;
            return stats;
        }

        public async Task<CustomerVipStatisticsDto?> GetStatisticsAsync(int customerId, CancellationToken ct = default)
        {
            var customer = await _context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);

            if (customer == null)
            {
                return null;
            }

            var stats = await BuildStatisticsAsync(customer, ct);
            stats.IsVip = customer.IsVip;
            return stats;
        }

        public async Task<CustomerVipStatisticsDto> RefreshVipStatusAsync(int customerId, bool ignoreManualOverride = false, CancellationToken ct = default)
        {
            var customer = await _context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId, ct)
                ?? throw new KeyNotFoundException($"Không tìm thấy khách hàng với ID {customerId}");

            var stats = await BuildStatisticsAsync(customer, ct);
            var hasManualOverride = HasManualOverride(customer);

            if (ignoreManualOverride && hasManualOverride)
            {
                SetManualOverride(customer, false);
                hasManualOverride = false;
            }

            var finalVip = (!ignoreManualOverride && hasManualOverride)
                ? customer.IsVip
                : stats.ComputedVip;

            customer.IsVip = finalVip;
            await _context.SaveChangesAsync(ct);

            stats.IsVip = finalVip;
            stats.IsManualOverride = hasManualOverride;
            return stats;
        }

        public async Task<CustomerVipStatisticsDto> UpdateVipStatusAsync(int customerId, bool isVip, CancellationToken ct = default)
        {
            var customer = await _context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId, ct)
                ?? throw new KeyNotFoundException($"Không tìm thấy khách hàng với ID {customerId}");

            customer.IsVip = isVip;
            SetManualOverride(customer, true);
            await _context.SaveChangesAsync(ct);

            var stats = await BuildStatisticsAsync(customer, ct);
            stats.IsVip = isVip;
            stats.IsManualOverride = true;
            return stats;
        }

        public async Task AutoUpdateVipWhenPaymentCompletedAsync(int orderId, CancellationToken ct = default)
        {
            try
            {
                var order = await _context.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.OrderId == orderId, ct);

                if (order?.CustomerId == null)
                {
                    return;
                }

                await RefreshVipStatusAsync(order.CustomerId.Value, ignoreManualOverride: false, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể auto cập nhật VIP cho order {OrderId}", orderId);
            }
        }

        private async Task<CustomerVipStatisticsDto> BuildStatisticsAsync(Customer customer, CancellationToken ct)
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CustomerId == customer.CustomerId)
                .ToListAsync(ct);

            var reservations = await _context.Reservations
                .AsNoTracking()
                .Where(r => r.CustomerId == customer.CustomerId)
                .ToListAsync(ct);

            var totalSpend = orders.Sum(o => o.TotalAmount ?? 0m);
            var totalBills = orders.Count;
            var totalGuests = reservations.Sum(r => r.NumberOfGuests);
            var reservationCount = reservations.Count;

            var averageSpendPerPerson = totalGuests > 0 ? totalSpend / totalGuests : 0m;
            var averageSpendPerBill = totalBills > 0 ? totalSpend / totalBills : 0m;

            var computedVip =
                averageSpendPerPerson >= 800000m ||
                averageSpendPerBill >= 1500000m ||
                totalSpend >= 20000000m ||
                totalBills >= 10 ||
                reservationCount >= 5;

            return new CustomerVipStatisticsDto
            {
                CustomerId = customer.CustomerId,
                FullName = customer.User?.FullName,
                Phone = customer.User?.Phone,
                TotalSpend = decimal.Round(totalSpend, 2),
                TotalBills = totalBills,
                TotalGuests = totalGuests,
                AverageSpendPerPerson = decimal.Round(averageSpendPerPerson, 2),
                AverageSpendPerBill = decimal.Round(averageSpendPerBill, 2),
                NumberOfReservations = reservationCount,
                ComputedVip = computedVip,
                IsVip = customer.IsVip,
                IsManualOverride = HasManualOverride(customer)
            };
        }

        private static bool HasManualOverride(Customer customer)
        {
            return !string.IsNullOrWhiteSpace(customer.Notes) &&
                   customer.Notes.Contains(ManualOverrideToken, StringComparison.OrdinalIgnoreCase);
        }

        private static void SetManualOverride(Customer customer, bool enabled)
        {
            var notes = customer.Notes ?? string.Empty;

            if (enabled)
            {
                if (!HasManualOverride(customer))
                {
                    customer.Notes = string.IsNullOrWhiteSpace(notes)
                        ? ManualOverrideToken
                        : $"{notes} {ManualOverrideToken}".Trim();
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(notes))
                {
                    customer.Notes = notes;
                    return;
                }

                var sanitized = notes.Replace(ManualOverrideToken, string.Empty, StringComparison.OrdinalIgnoreCase);
                while (sanitized.Contains("  "))
                {
                    sanitized = sanitized.Replace("  ", " ");
                }
                customer.Notes = sanitized.Trim();
            }
        }
    }
}

