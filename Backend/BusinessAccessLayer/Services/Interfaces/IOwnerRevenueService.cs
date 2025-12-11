using BusinessAccessLayer.DTOs.Owner;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface cho Owner Revenue Management
    /// </summary>
    public interface IOwnerRevenueService
    {
        /// <summary>
        /// Lấy dữ liệu revenue theo filter
        /// </summary>
        Task<RevenueResponseDto> GetRevenueDataAsync(RevenueFilterRequestDto request, CancellationToken ct = default);
    }
}

