using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    public class OrderTableRepository : Repository<Reservation>, IOrderTableRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public OrderTableRepository(SapaFoRestRmsContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByStatusAsync(string status)
        {
            return await _context.Reservations
                .Include(r => r.ReservationTables.Where(rt => rt.Reservation.Status == status))
                    .ThenInclude(rt => rt.Table)
                        .ThenInclude(t => t.Area)
                .Where(r => r.Status == status)
                .ToListAsync();
        }

        public async Task<(List<ReservationTable> Tables, int TotalCount)> GetPagedDistinctReservationTablesByStatusAsync(string status, int page, int pageSize)
        {
            var query = _context.ReservationTables
                .Include(rt => rt.Table)
                    .ThenInclude(t => t.Area)
                .Include(rt => rt.Reservation)
                .Where(rt => rt.Reservation.Status == status);

            // Lấy toàn bộ dữ liệu thỏa điều kiện 
            var allTables = await query.ToListAsync();

            // Group by TableId và chọn bản ghi đầu tiên
            var distinctTables = allTables
                .GroupBy(rt => rt.Table.TableId)
                .Select(g => g.First())
                .ToList();

            var totalCount = distinctTables.Count;

            // Phân trang trên bộ nhớ
            var pagedTables = distinctTables
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (pagedTables, totalCount);
        }

        public async Task<Reservation?> GetReservationByIdAndStatusAsync(int reservationId, string status)
        {
            return await _context.Reservations
                .Include(r => r.ReservationTables)
                    .ThenInclude(rt => rt.Table)
                        .ThenInclude(t => t.Area)
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId && r.Status == status);
        }

        // === SỬA HÀM NÀY ===
        public async Task<IEnumerable<MenuItem>> GetAvailableMenuWithCategoryAsync(int? categoryId, string? searchString)
        {
            var query = _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsAvailable == true)
                .AsQueryable(); // Bắt đầu query

            // 1. Lọc theo CategoryId (nếu có)
            if (categoryId.HasValue && categoryId > 0)
            {
                query = query.Where(m => m.CategoryId == categoryId.Value);
            }

            // 2. Lọc theo SearchString (nếu có)
            if (!string.IsNullOrEmpty(searchString))
            {
                // ToLower() để tìm kiếm không phân biệt hoa/thường
                query = query.Where(m => m.Name.ToLower().Contains(searchString.ToLower()));
            }

            return await query.ToListAsync();
        }

        // === THÊM HÀM NÀY VÀO CUỐI FILE ===
        public async Task<IEnumerable<MenuCategory>> GetAllCategoriesAsync()
        {
            return await _context.MenuCategories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<Table> GetByTbIdAsync(int tableId)
        {
            // Dùng FindAsync là nhanh nhất để lấy theo Khóa chính
            return await _context.Tables
                    .Include(t => t.Area) // <-- THÊM DÒNG NÀY
                    .FirstOrDefaultAsync(t => t.TableId == tableId);
        }

        public async Task<IEnumerable<Table>> GetAllWithAreaAsync()
        {
            // Dùng Include để lấy thông tin Area theo schema của bạn
            return await _context.Tables
                                 .Include(t => t.Area)
                                 .ToListAsync();
        }
        public IQueryable<Table> GetFilteredTables(string? searchString, string? areaName, int? floor)
        {
            var query = _context.Tables.Include(t => t.Area).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
                query = query.Where(t => t.TableNumber.Contains(searchString));

            if (!string.IsNullOrEmpty(areaName))
                query = query.Where(t => t.Area != null && t.Area.AreaName.Contains(areaName));

            if (floor.HasValue)
                query = query.Where(t => t.Area != null && t.Area.Floor == floor.Value);

            return query;
        }
        // Trong OrderTableRepository.cs
        public async Task<Reservation> GetActiveReservationByTableIdAsync(int tableId)
        {
            // === SỬ DỤNG TRẠNG THÁI để kích hoạt QR hoạt động ===
            string activeStatus = "Guest Seated";

            var reservationTable = await _context.ReservationTables
                .Include(rt => rt.Reservation)
                    .ThenInclude(r => r.Orders)
                        .ThenInclude(o => o.OrderDetails)
                .Where(rt => rt.TableId == tableId && rt.Reservation.Status == activeStatus)
                .FirstOrDefaultAsync();

            return reservationTable?.Reservation;
        }

        // === TRIỂN KHAI PHƯƠNG THỨC MỚI ===
        public async Task<IEnumerable<MenuItem>> GetMenuItemsByIdsAsync(List<int> menuItemIds)
        {
            return await _context.MenuItems
                .Where(m => menuItemIds.Contains(m.MenuItemId) && m.IsAvailable == true)
                .ToListAsync();
        }

        public async Task<List<string>> GetDistinctAreaNamesAsync()
        {
            return await _context.Areas
                .Select(a => a.AreaName)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync();
        }

        // (Trong DataAccessLayer/Repositories/OrderTableRepository.cs)

        public async Task<List<int?>> GetDistinctFloorsAsync()
        {
            // Dùng int? phòng trường hợp Floor bị null
            return await _context.Areas

                .Select(a => (int?)a.Floor) // Ép kiểu a.Floor (int) thành (int?)

                .Distinct()
                .OrderBy(f => f)
                .ToListAsync();
        }
    }
}
