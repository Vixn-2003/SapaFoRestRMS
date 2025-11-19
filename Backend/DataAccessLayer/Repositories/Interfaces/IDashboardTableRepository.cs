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

        // Chuyển trạng thái đơn đặt bàn
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

    }
}
