using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainAccessLayer.Models;

namespace DataAccessLayer.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface cho Counter Staff Order Management - UC123
    /// </summary>
    public interface ICounterStaffOrderRepository
    {
        /// <summary>
        /// Lấy danh sách orders theo filter
        /// </summary>
        Task<IEnumerable<Order>> GetAllOrdersAsync(string? statusFilter = null, DateOnly? date = null);

        /// <summary>
        /// Lấy order summary theo ID
        /// </summary>
        Task<Order?> GetOrderSummaryAsync(int orderId);
    }
}

