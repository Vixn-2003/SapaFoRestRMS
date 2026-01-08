using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.CounterStaff;

namespace BusinessAccessLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface cho Counter Staff Dashboard - UC122
    /// </summary>
    public interface ICounterStaffDashboardService
    {
        /// <summary>
        /// Lấy toàn bộ dữ liệu dashboard
        /// </summary>
        Task<CounterStaffDashboardDto> GetDashboardDataAsync(CancellationToken ct = default);
    }
}

