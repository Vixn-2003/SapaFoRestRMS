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
        /// <param name="statusFilter">Optional: Filter by item status (Pending, Cooking, Late, Ready). Null or empty = all</param>
        Task<List<KitchenOrderCardDto>> GetActiveOrdersAsync(string? statusFilter = null);

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
        /// Start cooking with specific quantity (split order detail if quantity < total)
        /// </summary>
        Task<StatusUpdateResponse> StartCookingWithQuantityAsync(StartCookingWithQuantityRequest request);

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
        /// <param name="statusFilter">Optional: Filter by item status (Pending, Cooking, Late, Ready). Null or empty = all</param>
        Task<List<GroupedMenuItemDto>> GetGroupedItemsByMenuItemAsync(string? statusFilter = null);

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
        /// Khôi phục (Recall) một order detail đã Done, đưa nó quay lại trạng thái Pending
        /// </summary>
        Task<StatusUpdateResponse> RecallOrderDetailAsync(RecallOrderDetailRequest request);

        /// <summary>
        /// Get order details with all items including Done status (for modal display)
        /// </summary>
        Task<KitchenOrderCardDto?> GetOrderDetailsWithAllItemsAsync(int orderId);

        /// <summary>
        /// Lấy thông tin order detail để in ticket khi hoàn thành món
        /// </summary>
        Task<PrintItemTicketDto?> GetOrderDetailForPrintAsync(int orderDetailId, int? orderComboItemId);

        /// <summary>
        /// Broadcast đơn mới đến tất cả màn hình bếp qua SignalR
        /// </summary>
        Task NotifyNewOrderAddedAsync(KitchenOrderCardDto order);

        /// <summary>
        /// Batch start cooking/update status cho nhiều món trong một lần
        /// </summary>
        Task<BatchCookResponse> BatchStartCookingAsync(BatchCookRequest request);
    }
}