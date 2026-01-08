using DataAccessLayer.Common;
using DomainAccessLayer.Models;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IDashboardTableRepository
    {
        // Trả về danh sách (Bàn, Reservation đang hoạt động (hoặc null))
        // Tuple (System.ValueTuple) là một cách tiện lợi để trả về nhiều
        // giá trị mà không cần tạo DTO riêng cho Repository.
        Task<List<(Table Table, Reservation ActiveReservation)>> GetFilteredTablesWithStatusAsync(string? areaName, int? floor, string? searchString);

        // Chuyển trạng thái đơn đặt bàn danh sách
        Task<PagedList<Reservation>> GetPagedReservationsAsync(ReservationQueryParameters parameters);

        // Thay đổi: Guid -> int
        Task<Reservation?> GetReservationDetailByIdAsync(int reservationId);

        // Thay đổi: Guid -> int
        Task<Reservation?> GetReservationForUpdateAsync(int reservationId);

        void Update(Reservation reservation);


        // Lấy thông tin cơ bản của bàn (và vị trí)
        Task<Table> GetTableInfoAsync(int tableId);

        // Lấy Reservation đang active (cùng các món đã gọi)
        Task<Reservation> GetActiveReservationForTableAsync(int tableId);

        // Lấy toàn bộ MenuItems
        Task<List<MenuItem>> GetActiveMenuItemsAsync();

        // Lấy toàn bộ Combos
        Task<List<Combo>> GetActiveCombosAsync();
        Task<IEnumerable<MenuCategory>> GetCategoriesAsync();

        /// <summary>
        /// //////////////////
        /// </summary> cập nhập POS
        /// <param name="tableId"></param>
        /// <returns></returns>
        // 1. Tìm đơn đặt bàn (Reservation) đang hoạt động của bàn này
        Task<Reservation?> GetActiveReservationByTableIdAsync(int tableId);

        // 2. Tìm Hóa đơn (Order) gắn với Reservation này (Để biết đã có hóa đơn chưa)
        Task<Order?> GetOrderByReservationIdAsync(int reservationId);

        // 3. Tạo Hóa đơn mới (Nếu chưa có)
        Task AddOrderAsync(Order order);

        // 4. Lấy thông tin Món ăn / Combo (Để lấy giá gốc bảo mật)
        Task<MenuItem?> GetMenuItemAsync(int id);
        Task<Combo?> GetComboAsync(int id);

        // 5. Lấy chi tiết món đã gọi (Để sửa/xóa)
        Task<OrderDetail?> GetOrderDetailByIdAsync(int orderDetailId);

        // 6. Thêm món ăn mới vào Hóa đơn
        Task AddOrderDetailAsync(OrderDetail item);

        // Thêm hàm này vào Interface
        Task UpdateOrderDetailAsync(OrderDetail item);

        // 7. Lưu tất cả thay đổi xuống DB
        Task<bool> SaveChangesAsync();


        // 1. Hàm lấy danh sách món con trong Combo cấu hình
        Task<List<ComboItem>> GetComboItemsByComboIdAsync(int comboId);

        // 2. Hàm thêm món con vào hóa đơn thực tế
        Task AddOrderComboItemAsync(OrderComboItem item);
        // Lấy danh sách OrderComboItems thuộc về một OrderDetail cụ thể
        Task<List<OrderComboItem>> GetOrderComboItemsByOrderDetailIdAsync(int orderDetailId);
    }
}
