using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebSapaForestForStaff.DTOs.CounterStaff;

namespace WebSapaForestForStaff.Services.Api.Interfaces
{
    /// <summary>
    /// Interface for Counter Staff Order API Service
    /// </summary>
    public interface ICounterStaffOrderApiService
    {
        /// <summary>
        /// Lấy danh sách orders theo filter
        /// </summary>
        Task<List<OrderListItemDto>?> GetOrdersAsync(
            string? status = null,
            DateOnly? date = null,
            string? tableNumber = null,
            string? searchKeyword = null);

        /// <summary>
        /// Lấy order summary theo ID
        /// </summary>
        Task<OrderListItemDto?> GetOrderSummaryAsync(int orderId);
    }
}

