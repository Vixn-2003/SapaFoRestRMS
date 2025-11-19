using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.DTOs.OrderGuest;
using BusinessAccessLayer.DTOs.OrderGuest.ListOrder;
using DataAccessLayer.Common;
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
        Task SeatGuestAsync(int reservationId);

        Task<StaffOrderScreenDto> GetStaffOrderScreenAsync(int tableId, int? categoryId, string? searchString);
        Task<List<CategoryDto>> GetAllCategoriesAsync();

    }
}
