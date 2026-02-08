using System.Threading.Tasks;
using WebSapaForestForStaff.DTOs.CounterStaff;

namespace WebSapaForestForStaff.Services.Api.Interfaces
{
    /// <summary>
    /// Interface for Counter Staff Dashboard API Service
    /// </summary>
    public interface ICounterStaffDashboardApiService
    {
        /// <summary>
        /// Lấy toàn bộ dữ liệu dashboard
        /// </summary>
        Task<CounterStaffDashboardDto?> GetDashboardAsync();
    }
}

