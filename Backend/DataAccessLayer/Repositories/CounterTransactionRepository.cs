using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    /// <summary>
    /// Repository implementation cho Counter Transaction History - UC124
    /// </summary>
    public class CounterTransactionRepository : ICounterTransactionRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public CounterTransactionRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Transaction> Transactions, int TotalCount)> GetTransactionHistoryAsync(
            DateOnly? fromDate,
            DateOnly? toDate,
            string? paymentMethod,
            string? status,
            int pageNumber,
            int pageSize)
        {
            var query = BuildTransactionQuery(fromDate, toDate, paymentMethod, status);

            var totalCount = await query.CountAsync();

            var transactions = await query
                .OrderByDescending(t => t.CompletedAt ?? t.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (transactions, totalCount);
        }

        public async Task<IEnumerable<Transaction>> ExportTransactionsToListAsync(
            DateOnly? fromDate,
            DateOnly? toDate,
            string? paymentMethod,
            string? status)
        {
            var query = BuildTransactionQuery(fromDate, toDate, paymentMethod, status);

            return await query
                .OrderByDescending(t => t.CompletedAt ?? t.CreatedAt)
                .ToListAsync();
        }

        private IQueryable<Transaction> BuildTransactionQuery(
            DateOnly? fromDate,
            DateOnly? toDate,
            string? paymentMethod,
            string? status)
        {
            var query = _context.Transactions
                .Include(t => t.Order)
                    .ThenInclude(o => o.Reservation)
                        .ThenInclude(r => r!.ReservationTables)
                            .ThenInclude(rt => rt.Table)
                .Include(t => t.ConfirmedByUser)
                .AsQueryable();

            // Filter by date range
            if (fromDate.HasValue)
            {
                var from = fromDate.Value.ToDateTime(TimeOnly.MinValue);
                query = query.Where(t => (t.CompletedAt ?? t.CreatedAt) >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.ToDateTime(TimeOnly.MaxValue);
                query = query.Where(t => (t.CompletedAt ?? t.CreatedAt) <= to);
            }

            // Filter by payment method
            if (!string.IsNullOrWhiteSpace(paymentMethod))
            {
                query = query.Where(t => t.PaymentMethod == paymentMethod);
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }

            return query;
        }
    }
}

