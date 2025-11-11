using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces; // (Cần cho cả 2 repo)
using System.Linq;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class DashboardTableService : IDashboardTableService
    {
        private readonly IDashboardTableRepository _dashboardRepo;
        private readonly IOrderTableRepository _orderTableRepo; // (Dùng để lấy Filters)

        public DashboardTableService(IDashboardTableRepository dashboardRepo, IOrderTableRepository orderTableRepo)
        {
            _dashboardRepo = dashboardRepo;
            _orderTableRepo = orderTableRepo;
        }

        public async Task<DashboardDataDto> GetDashboardDataAsync(string? areaName, int? floor, string? status, string? searchString, int page, int pageSize)
        {
            var dashboardData = new DashboardDataDto();

            // 1. Gọi Repo (Repo chỉ lọc Area, Floor, Search)
            var allTablesWithStatus = await _dashboardRepo.GetFilteredTablesWithStatusAsync(areaName, floor, searchString);

            // 2. Chuyển đổi (Map) sang DTO (bao gồm cả thời gian vào)
            var allTableDtos = allTablesWithStatus.Select(data => new TableDashboardDto
            {
                TableId = data.Table.TableId,
                TableNumber = data.Table.TableNumber,
                AreaName = data.Table.Area.AreaName,
                Floor = data.Table.Area.Floor,
                Capacity = data.Table.Capacity,
                Status = (data.ActiveReservation != null) ? "Active" : "Available",
                GuestCount = (data.ActiveReservation != null) ? data.ActiveReservation.NumberOfGuests : 0,

                // (MỚI) Lấy thời gian khách vào (ReservationTime)
                GuestSeatedTime = (data.ActiveReservation != null) ? (DateTime?)data.ActiveReservation.ReservationTime : null
            }).ToList();

            // 3. (MỚI) Lọc theo Status (Lọc trên danh sách DTO)
            if (!string.IsNullOrEmpty(status))
            {
                allTableDtos = allTableDtos.Where(t => t.Status == status).ToList();
            }

            // 4. (MỚI) Lấy tổng số lượng (SAU KHI LỌC) để phân trang
            dashboardData.TotalCount = allTableDtos.Count;

            // 5. (MỚI) Phân trang
            dashboardData.Tables = allTableDtos
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 6. Lấy dữ liệu cho bộ lọc (Như cũ)
            dashboardData.AreaNames = await _orderTableRepo.GetDistinctAreaNamesAsync();
            dashboardData.Floors = await _orderTableRepo.GetDistinctFloorsAsync();

            return dashboardData;
        }
    }
}