using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class OrderTableService : IOrderTableService
    {
        private readonly IOrderTableRepository _orderTableRepository;

        public OrderTableService(IOrderTableRepository orderTableRepository)
        {
            _orderTableRepository = orderTableRepository;
        }

        public async Task<IEnumerable<TableOrderDto>> GetTablesByReservationStatusAsync(string status)
        {
            var reservations = await _orderTableRepository.GetReservationsByStatusAsync(status);

            return reservations
                .SelectMany(r => r.ReservationTables.Select(rt => new TableOrderDto
                {
                    TableId = rt.Table.TableId,
                    TableNumber = rt.Table.TableNumber,
                    Capacity = rt.Table.Capacity,
                    Status = rt.Table.Status,
                    AreaName = rt.Table.Area?.AreaName,
                    Floor = rt.Table.Area?.Floor,
                    NumberGuest = r.NumberOfGuests //  lấy từ Reservation
                }))
                .DistinctBy(t => t.TableId) // tránh trùng bàn nếu có nhiều ReservationTable
                .ToList();
        }



        public async Task<PagedTableOrderResult> GetTablesByReservationStatusAsync(string status, int page, int pageSize)
        {
            var (reservationTables, totalCount) =
                await _orderTableRepository.GetPagedDistinctReservationTablesByStatusAsync(status, page, pageSize);

            var dtos = reservationTables.Select(rt => new TableOrderDto
            {
                TableId = rt.Table.TableId,
                TableNumber = rt.Table.TableNumber,
                Capacity = rt.Table.Capacity,
                Status = rt.Table.Status,
                AreaName = rt.Table.Area?.AreaName,
                Floor = rt.Table.Area?.Floor,
                NumberGuest = rt.Reservation.NumberOfGuests
            }).ToList();

            return new PagedTableOrderResult
            {
                TotalCount = totalCount,
                Items = dtos,
                Page = page,
                PageSize = pageSize
            };
        }
        public async Task<IEnumerable<TableOrderDto>> GetTablesByReservationIdAndStatusAsync(int reservationId, string status)
        {
            var reservation = await _orderTableRepository.GetReservationByIdAndStatusAsync(reservationId, status);

            if (reservation == null)
                return Enumerable.Empty<TableOrderDto>();

            return reservation.ReservationTables.Select(rt => new TableOrderDto
            {
                TableId = rt.Table.TableId,
                TableNumber = rt.Table.TableNumber,
                Capacity = rt.Table.Capacity,
                Status = rt.Table.Status,
                AreaName = rt.Table.Area?.AreaName,
                Floor = rt.Table.Area?.Floor,
                NumberGuest = reservation.NumberOfGuests // lấy từ Reservation
            }).ToList();
        }

        public async Task<IEnumerable<MenuItemDto>> GetMenuForReservationAsync(int reservationId, string status)
        {
            // Kiểm tra reservation hợp lệ
            var reservation = await _orderTableRepository.GetReservationByIdAndStatusAsync(reservationId, status);
            if (reservation == null)
                throw new Exception("Reservation not found or invalid status");

            // Lấy danh sách món khả dụng
            var menuItems = await _orderTableRepository.GetAvailableMenuWithCategoryAsync();

            // Lấy danh sách món đã gọi (nếu có)
            var orderedItems = reservation.Orders
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => od.MenuItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            // Map sang DTO
            return menuItems.Select(m => new MenuItemDto
            {
                MenuItemId = m.MenuItemId,
                Name = m.Name,
                CategoryName = m.Category?.CategoryName ?? "",
                Price = m.Price,
                ImageUrl = m.ImageUrl,
                Quantity = orderedItems.ContainsKey(m.MenuItemId) ? orderedItems[m.MenuItemId] : 0
            }).ToList();
        }

        public class MenuItemDto
        {
            public int MenuItemId { get; set; }

            public string Name { get; set; } = string.Empty;

            public string CategoryName { get; set; } = string.Empty;

            public decimal Price { get; set; }

            public string CourseType { get; set; } = string.Empty;

            public bool IsAvailable { get; set; }

            public string? ImageUrl { get; set; }

            // Số lượng món đã gọi cho bàn này (0 nếu chưa gọi)
            public int Quantity { get; set; }

            // Tổng tiền tạm tính cho món này (Price * Quantity)
            public decimal Total => Price * Quantity;
        }
        public class TableOrderDto
        {
            public int TableId { get; set; }
            public string TableNumber { get; set; }
            public int Capacity { get; set; }
            public string? Status { get; set; }
            public string? AreaName { get; set; }
            public int? Floor {  get; set; }
            public int NumberGuest { get; set; }
        }
        public class PagedTableOrderResult
        {
            public int TotalCount { get; set; }              // Tổng số bàn sau Distinct
            public List<TableOrderDto> Items { get; set; } = new(); // Danh sách bàn trang hiện tại
            public int Page { get; set; }
            public int PageSize { get; set; }
        }


    }
}

