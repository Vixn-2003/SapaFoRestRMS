using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    /// <summary>
    /// Repository implementation - CHỈ làm việc với Domain Models
    /// </summary>
    public class CustomerManagementRepository : ICustomerManagementRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public CustomerManagementRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get customer by ID with basic includes
        /// </summary>
        public async Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken ct = default)
        {
            return await _context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && 
                                        (c.User == null || c.User.IsDeleted != true), ct);
        }

        /// <summary>
        /// Get customer with all orders, order details, and payments
        /// </summary>
        public async Task<Customer?> GetCustomerWithOrdersAsync(int customerId, CancellationToken ct = default)
        {
            return await _context.Customers
                .Include(c => c.User)
                .Include(c => c.Orders.Where(o => o.Status == "Completed" || o.Status == "Paid"))
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.MenuItem)
                .Include(c => c.Orders.Where(o => o.Status == "Completed" || o.Status == "Paid"))
                    .ThenInclude(o => o.Payments)
                .Include(c => c.Reservations)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && 
                                        (c.User == null || c.User.IsDeleted != true), ct);
        }

        /// <summary>
        /// Get customers query for filtering - returns IQueryable for service layer to process
        /// </summary>
        public async Task<(IQueryable<Customer> Query, int TotalCount)> GetCustomersQueryAsync(
            string? searchKeyword,
            bool? isVipOnly,
            decimal? minSpending,
            decimal? maxSpending,
            int? minVisits,
            string sortBy,
            string sortDirection,
            CancellationToken ct = default)
        {
            var query = _context.Customers
                .Include(c => c.User)
                .Include(c => c.Orders.Where(o => o.Status == "Completed" || o.Status == "Paid"))
                    .ThenInclude(o => o.Payments)
                .Where(c => c.User != null && c.User.IsDeleted != true)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                var keyword = searchKeyword.Trim().ToLower();
                query = query.Where(c => 
                    c.User.FullName.ToLower().Contains(keyword) ||
                    (c.User.Phone != null && c.User.Phone.Contains(keyword)) ||
                    c.User.Email.ToLower().Contains(keyword));
            }

            // Apply VIP filter
            if (isVipOnly.HasValue)
            {
                query = query.Where(c => c.IsVip == isVipOnly.Value);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync(ct);

            return (query, totalCount);
        }

        /// <summary>
        /// Update VIP status
        /// </summary>
        public async Task<bool> UpdateVipStatusAsync(int customerId, bool isVip, CancellationToken ct = default)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);

            if (customer == null)
                return false;

            customer.IsVip = isVip;
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        /// <summary>
        /// Check if customer exists
        /// </summary>
        public async Task<bool> CustomerExistsAsync(int customerId, CancellationToken ct = default)
        {
            return await _context.Customers
                .AnyAsync(c => c.CustomerId == customerId && 
                             (c.User == null || c.User.IsDeleted != true), ct);
        }
    }
}
