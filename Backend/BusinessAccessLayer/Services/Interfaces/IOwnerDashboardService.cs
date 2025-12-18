using BusinessAccessLayer.DTOs.Owner;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface cho Owner Dashboard
    /// </summary>
    public interface IOwnerDashboardService
    {
        /// <summary>
        /// Lấy toàn bộ dữ liệu dashboard cho Owner
        /// </summary>
        Task<OwnerDashboardDto> GetDashboardDataAsync(CancellationToken ct = default);
    }
}

