using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface cho Customer Management Module
    /// CHỈ làm việc với Domain Models, KHÔNG có DTOs
    /// </summary>
    public interface ICustomerManagementRepository
    {
        /// <summary>
        /// Get customer by ID with related data
        /// </summary>
        Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken ct = default);

        /// <summary>
        /// Get all customers with filters (returns domain models only)
        /// </summary>
        Task<(IQueryable<Customer> Query, int TotalCount)> GetCustomersQueryAsync(
            string? searchKeyword,
            bool? isVipOnly,
            decimal? minSpending,
            decimal? maxSpending,
            int? minVisits,
            int? maxVisits,
            string sortBy,
            string sortDirection,
            CancellationToken ct = default);

        /// <summary>
        /// Update VIP status
        /// </summary>
        Task<bool> UpdateVipStatusAsync(int customerId, bool isVip, CancellationToken ct = default);

        /// <summary>
        /// Check if customer exists
        /// </summary>
        Task<bool> CustomerExistsAsync(int customerId, CancellationToken ct = default);

        /// <summary>
        /// Get customer with all related orders and payments
        /// </summary>
        Task<Customer?> GetCustomerWithOrdersAsync(int customerId, CancellationToken ct = default);

        /// <summary>
        /// Update customer profile information
        /// </summary>
        Task UpdateCustomerAsync(Customer customer, CancellationToken ct = default);
    }
}
