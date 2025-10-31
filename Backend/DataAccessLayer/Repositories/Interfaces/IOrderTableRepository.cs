using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IOrderTableRepository : IRepository<Reservation>
    {
        /// <summary>
        /// Lấy danh sách Reservation theo trạng thái (ví dụ: "Guest Seated", "Completed").
        /// </summary>
        /// <param name="status">Trạng thái đặt bàn.</param>
        Task<IEnumerable<Reservation>> GetReservationsByStatusAsync(string status);

        Task<(List<ReservationTable> Tables, int TotalCount)> GetPagedDistinctReservationTablesByStatusAsync(string status, int page, int pageSize);

        Task<Reservation?> GetReservationByIdAndStatusAsync(int reservationId, string status); // lấy bàn khách theo id của reservation

        //Lấy danh sách menu cùng vs category
        Task<IEnumerable<MenuItem>> GetAvailableMenuWithCategoryAsync(int? categoryId, string? searchString);
        Task<IEnumerable<MenuCategory>> GetAllCategoriesAsync();
        // === 2 HÀM LẤY BỘ LỌC ===
        Task<List<string>> GetDistinctAreaNamesAsync();
        Task<List<int?>> GetDistinctFloorsAsync();


        // tạo QR cho khách quét
        Task<Table> GetByTbIdAsync(int tableId);
        Task<IEnumerable<Table>> GetAllWithAreaAsync();
        IQueryable<Table> GetFilteredTables(string? searchString, string? areaName, int? floor);

        Task<Reservation> GetActiveReservationByTableIdAsync(int tableId);

        // === THÊM PHƯƠNG THỨC MỚI NÀY ===
        /// <summary>
        /// Lấy thông tin các món ăn từ một danh sách ID
        /// </summary>
        Task<IEnumerable<MenuItem>> GetMenuItemsByIdsAsync(List<int> menuItemIds);

        //Gọi hỗ trợ
        Task<bool> HasPendingAssistanceRequestAsync(int tableId);
        Task CreateAssistanceRequestAsync(AssistanceRequest request);
    }


}
