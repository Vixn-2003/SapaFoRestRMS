using BusinessAccessLayer.DTOs.Waiter;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IWaiterOrderTrackingService
    {
        /// <summary>
        /// Lấy danh sách orders để theo dõi tiến độ phục vụ
        /// </summary>
        Task<WaiterOrderTrackingDto> GetOrderTrackingAsync(int? waiterUserId = null);

        /// <summary>
        /// Yêu cầu làm gấp một món
        /// </summary>
        Task<RequestUrgentResponse> RequestUrgentAsync(RequestUrgentDto request);

        /// <summary>
        /// Hủy món (chưa nấu) - Waiter có quyền hủy, không tính tiền
        /// </summary>
        Task<CancelOrderDetailResponse> CancelOrderDetailAsync(CancelOrderDetailDto request);

        /// <summary>
        /// Đánh dấu món đã phục vụ (lấy món)
        /// </summary>
        Task<MarkAsServedResponse> MarkAsServedAsync(MarkAsServedDto request);
    }
}

