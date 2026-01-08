using WebSapaForestForStaff.DTOs.Owner;
using System.Threading;
using System.Threading.Tasks;

namespace WebSapaForestForStaff.Services.Api.Interfaces
{
    /// <summary>
    /// API Service interface cho Owner Warehouse Alert
    /// </summary>
    public interface IOwnerWarehouseAlertApiService
    {
        /// <summary>
        /// Lấy toàn bộ dữ liệu cảnh báo kho
        /// </summary>
        Task<(bool success, WarehouseAlertResponseDto? data, string? message)> GetWarehouseAlertsAsync(CancellationToken ct = default);

        /// <summary>
        /// Lấy tóm tắt cảnh báo kho
        /// </summary>
        Task<(bool success, AlertSummaryCardsDto? data, string? message)> GetWarehouseAlertSummaryAsync(CancellationToken ct = default);
    }
}

