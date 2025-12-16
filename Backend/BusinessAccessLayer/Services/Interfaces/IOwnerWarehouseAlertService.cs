using BusinessAccessLayer.DTOs.Owner;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface cho Owner Warehouse Alert Management
    /// </summary>
    public interface IOwnerWarehouseAlertService
    {
        /// <summary>
        /// Lấy toàn bộ dữ liệu cảnh báo kho
        /// </summary>
        Task<WarehouseAlertResponseDto> GetWarehouseAlertsAsync(CancellationToken ct = default);
    }
}

