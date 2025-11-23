using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.Kitchen;

namespace BusinessAccessLayer.Services
{
    public interface IKitchenDisplayService
    {
        /// <summary>
        /// Get all active orders for Sous Chef KDS screen
        /// </summary>
        Task<List<KitchenOrderCardDto>> GetActiveOrdersAsync();

        /// <summary>
        /// Get orders filtered by specific course type (for station screens)
        /// </summary>
        Task<List<KitchenOrderCardDto>> GetOrdersByCourseTypeAsync(string courseType);

        /// <summary>
        /// Update status of a single item (called from station screen)
        /// This will trigger real-time update to Sous Chef screen
        /// </summary>
        Task<StatusUpdateResponse> UpdateItemStatusAsync(UpdateItemStatusRequest request);

        /// <summary>
        /// Mark entire order as completed (called by Sous Chef)
        /// </summary>
        Task<StatusUpdateResponse> CompleteOrderAsync(CompleteOrderRequest request);

        /// <summary>
        /// Get all available course types for filtering
        /// </summary>
        Task<List<string>> GetCourseTypesAsync();

        /// <summary>
        /// Get grouped items by menu item (theo từng món) - nhóm tất cả các món ăn từ tất cả các order
        /// </summary>
        Task<List<GroupedMenuItemDto>> GetGroupedItemsByMenuItemAsync();

        /// <summary>
        /// Get station items by category name (theo MenuCategory) - có 2 luồng: tất cả và urgent
        /// </summary>
        Task<StationItemsResponse> GetStationItemsByCategoryAsync(string categoryName);

        /// <summary>
        /// Mark order detail as urgent/not urgent (yêu cầu từ bếp phó)
        /// </summary>
        Task<StatusUpdateResponse> MarkAsUrgentAsync(MarkAsUrgentRequest request);

        /// <summary>
        /// Get all menu categories for stations
        /// </summary>
        Task<List<string>> GetStationCategoriesAsync();

        /// <summary>
        /// Lấy danh sách các order đã hoàn thành gần đây (trong X phút)
        /// </summary>
        Task<List<KitchenOrderCardDto>> GetRecentlyFulfilledOrdersAsync(int minutesAgo = 10);

        /// <summary>
        /// Khôi phục (Recall) một order detail đã Done, đưa nó quay lại trạng thái Processing
        /// </summary>
        Task<StatusUpdateResponse> RecallOrderDetailAsync(RecallOrderDetailRequest request);
    }
}