using WebSapaForestForStaff.DTOs.Owner;
using System.Threading;
using System.Threading.Tasks;

namespace WebSapaForestForStaff.Services.Api.Interfaces
{
    /// <summary>
    /// API Service interface cho Owner Dashboard
    /// </summary>
    public interface IOwnerDashboardApiService
    {
        /// <summary>
        /// Lấy dữ liệu dashboard
        /// </summary>
        Task<(bool success, OwnerDashboardDto? data, string? message)> GetDashboardDataAsync(CancellationToken ct = default);
    }
}

