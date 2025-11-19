using static BusinessAccessLayer.Services.OrderTableService;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IOrderTableService
    {
        Task<IEnumerable<TableOrderDto>> GetTablesByReservationStatusAsync(string status);

        Task<PagedTableOrderResult> GetTablesByReservationStatusAsync(string status, int page, int pageSize);

        Task<IEnumerable<TableOrderDto>> GetTablesByReservationIdAndStatusAsync(int reservationId, string status);

        Task<IEnumerable<MenuItemDto>> GetMenuForReservationAsync(int reservationId, string status, int? categoryId, string? searchString);

        Task<IEnumerable<MenuCategoryDto>> GetMenuCategoriesAsync();

        // === 2 HÀM LẤY BỘ LỌC ===
        Task<List<string>> GetAreaNamesAsync();
        Task<List<int?>> GetFloorsAsync();
        //tạo QR cho khách hàng
        Task<IEnumerable<TableQRDTO>> GetTablesAsync(
    int page, int pageSize,
    string? searchString, string? areaName, int? floor);

        Task<int> GetTotalCountAsync(string? searchString, string? areaName, int? floor);

        Task<IEnumerable<TableQRDTO>> GetAllTablesAsync(); // Dùng DTO để trả về
        Task<byte[]> GenerateQrCodeForTableAsync(int tableId);

        // === THÊM HÀM MỚI NÀY ===

        Task<MenuPageViewModel> GetMenuForTableAsync(int tableId, int? categoryId, string? searchString);
        // === THÊM HÀM MỚI NÀY ===
        Task<bool> CancelOrderItemAsync(int orderDetailId);

        // === THÊM HÀM MỚI NÀY ===
        /// <summary>
        /// Nhận giỏ hàng từ khách và tạo Order
        /// </summary>
        Task<OrderResultDto> SubmitOrderAsync(SubmitOrderRequest orderDto);
        //Gọi xử lý sự cố
        Task RequestAssistanceAsync(AssistanceRequestDto requestDto);

        Task<ComboDetailDto> GetComboDetailsAsync(int comboId);

        Task<MenuItemDetailDto> GetMenuItemDetailsAsync(int menuItemId);
    }
}
