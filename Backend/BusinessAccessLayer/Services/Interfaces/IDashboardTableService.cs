using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.DTOs.OrderGuest;
using BusinessAccessLayer.DTOs.OrderGuest.ListOrder;
using DataAccessLayer.Common;
using DomainAccessLayer.Models;
using static BusinessAccessLayer.Services.OrderTableService;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IDashboardTableService
    {
        // Nhận bộ lọc, trả về DTO chứa tất cả dữ liệu
        Task<DashboardDataDto> GetDashboardDataAsync(string? areaName, int? floor, string? status, string? searchString, int page, int pageSize);

        Task<PagedList<ReservationListDto>> GetReservationsAsync(ReservationQueryParameters parameters);

        // Thay đổi: Guid -> int
        Task<ReservationDetailDto> GetReservationDetailAsync(int reservationId);

        // Thay đổi: Guid -> int
        Task<Reservation> SeatGuestAsync(int id);
        Task<StaffOrderScreenDto> GetStaffOrderScreenAsync(int tableId, int? categoryId, string? searchString);
        Task<List<CategoryDto>> GetAllCategoriesAsync();


        //new waiter
        Task SaveOrderChangesAsync(SaveOrderRequest request);
        public class OrderDetailDto
        {
            public int OrderDetailId { get; set; }
            public string? ItemName { get; set; }
            public string ItemType { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public string? Status { get; set; }
            public string? Notes { get; set; }
        }

        public class OrderItemUpdateDto
        {
            public string Action { get; set; } = null!; // "Add", "Update", "Delete"
            public int? OrderDetailId { get; set; } // Chỉ dùng cho Update/Delete
            public int? MenuItemId { get; set; } // Chỉ dùng cho Add
            public int? ComboId { get; set; } // Chỉ dùng cho Add
            public int Quantity { get; set; }
            public string? Notes { get; set; }
        }

    }
}
