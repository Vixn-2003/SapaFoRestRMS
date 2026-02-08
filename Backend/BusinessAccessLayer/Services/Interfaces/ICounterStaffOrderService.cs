using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.CounterStaff;

namespace BusinessAccessLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface cho Counter Staff Order Management - UC123
    /// </summary>
    public interface ICounterStaffOrderService
    {
        /// <summary>
        /// Lấy danh sách orders theo filter
        /// </summary>
        Task<List<OrderListItemDto>> GetAllOrdersAsync(OrderListFilterDto filter, CancellationToken ct = default);

        /// <summary>
        /// Lấy order summary theo ID
        /// </summary>
        Task<OrderListItemDto?> GetOrderSummaryAsync(int orderId, CancellationToken ct = default);
    }
}

