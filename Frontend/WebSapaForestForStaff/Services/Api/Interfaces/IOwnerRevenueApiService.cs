using WebSapaForestForStaff.DTOs.Owner;
using System.Threading;
using System.Threading.Tasks;

namespace WebSapaForestForStaff.Services.Api.Interfaces
{
    /// <summary>
    /// API Service interface cho Owner Revenue
    /// </summary>
    public interface IOwnerRevenueApiService
    {
        /// <summary>
        /// Lấy dữ liệu revenue theo filter
        /// </summary>
        Task<(bool success, RevenueResponseDto? data, string? message)> GetRevenueDataAsync(RevenueFilterRequestDto request, CancellationToken ct = default);

        /// <summary>
        /// Lấy tóm tắt revenue (30 ngày gần nhất)
        /// </summary>
        Task<(bool success, RevenueResponseDto? data, string? message)> GetRevenueSummaryAsync(CancellationToken ct = default);
    }
}

