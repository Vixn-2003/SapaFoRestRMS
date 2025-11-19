using DataAccessLayer.Common;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class DashboardTableRepository : IDashboardTableRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public DashboardTableRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<List<(Table Table, Reservation ActiveReservation)>> GetFilteredTablesWithStatusAsync(string? areaName, int? floor, string? searchString)
        {
            var query = _context.Tables.Include(t => t.Area).Include(t => t.ReservationTables)
                .ThenInclude(t => t.Reservation)
                .AsQueryable();

            if (floor.HasValue)
            {
                query = query.Where(t => t.Area.Floor == floor.Value);
            }
            if (!string.IsNullOrEmpty(areaName))
            {
                query = query.Where(t => t.Area.AreaName == areaName);
            }

            // (MỚI) Thêm logic tìm kiếm theo tên/số bàn
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(t => t.TableNumber.Contains(searchString));
            }

            var projectedResult = await query
                .OrderBy(t => t.TableNumber)
                .Select(t => new {
                    Table = t,
                    ActiveReservation = t.ReservationTables
                        .Select(rt => rt.Reservation)
                        .Where(r => r.Status == "Guest Seated" || r.Status == "Confirmed")
                        .OrderByDescending(r => r.ReservationTime)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var result = projectedResult.Select(data =>
                (data.Table, data.ActiveReservation)
            ).ToList();

            return result;
        }

        // (1) Lấy danh sách (Đã cập nhật logic sắp xếp)
        // (1) Lấy danh sách (Đã cập nhật logic LỌC và SẮP XẾP)
        public async Task<PagedList<Reservation>> GetPagedReservationsAsync(ReservationQueryParameters parameters)
        {
            var query = _context.Reservations
                .Include(r => r.Customer)
                    .ThenInclude(c => c.User)
                .Include(r => r.ReservationTables)
                    .ThenInclude(rt => rt.Table)
                        .ThenInclude(t => t.Area)
                .AsQueryable();

            // ⭐️ BẮT ĐẦU THAY ĐỔI LOGIC LỌC (FILTER) ⭐️

            // Kiểm tra xem người dùng có đang lọc "All" hay không
            bool isFilteringAll = string.IsNullOrEmpty(parameters.Status) || parameters.Status.ToLower() == "all";

            if (isFilteringAll)
            {
                // Yêu cầu: Khi xem "All", không hiển thị "Pending"
                query = query.Where(r => r.Status != "Pending");
            }
            else
            {
                // Người dùng đang lọc 1 status cụ thể (ví dụ: "Confirmed", hoặc "Pending")
                // Nếu họ cố tình lọc "Pending", họ vẫn sẽ thấy nó.
                query = query.Where(r => r.Status == parameters.Status);
            }

            // ⭐️ KẾT THÚC THAY ĐỔI LOGIC LỌC ⭐️

            // Search... (giữ nguyên)
            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var searchTerm = parameters.SearchTerm.ToLower();
                query = query.Where(r =>
                    (r.Customer != null && r.Customer.User != null &&
                        (r.Customer.User.FullName.ToLower().Contains(searchTerm) || r.Customer.User.Phone.Contains(searchTerm))) ||
                    (r.CustomerNameReservation != null && r.CustomerNameReservation.ToLower().Contains(searchTerm))
                );
            }

            // ⭐️ BẮT ĐẦU THAY ĐỔI LOGIC SẮP XẾP (SORT) ⭐️

            var now = DateTime.Now;
            var nowDate = now.Date;
            var nowTime = now.TimeOfDay;

            query = query
                // 1. Ưu tiên "Confirmed" (Nhóm 0)
                .OrderBy(r => r.Status == "Confirmed" ? 0 :
                              // 2. "Guest Seated" (Nhóm 1) - Sẽ đứng sau Confirmed
                              r.Status == "Guest Seated" ? 1 :
                              // 3. Các status khác (Nhóm 2)
                              2)

                // 4. Sắp xếp BÊN TRONG nhóm "Confirmed" (Nhóm 0):
                //    Ưu tiên "Sắp diễn ra" (Future)
                .ThenBy(r => (r.ReservationDate < nowDate) || (r.ReservationDate == nowDate && r.ReservationTime.TimeOfDay < nowTime) ? 1 : 0) // 0 = Future, 1 = Past
                                                                                                                                               //    Sắp xếp "Future" (sát giờ nhất lên đầu)
                .ThenBy(r => r.ReservationDate)
                .ThenBy(r => r.ReservationTime.TimeOfDay)

                // 5. Sắp xếp BÊN TRONG nhóm "Guest Seated" (Nhóm 1):
                //    Ưu tiên theo "Giờ vào" (ArrivalAt) mới nhất
                .ThenByDescending(r => r.ArrivalAt)

                // 6. Sắp xếp các nhóm còn lại (Nhóm 2 - Completed, Cancelled...):
                //    Sắp xếp theo ngày/giờ đặt mới nhất
                .ThenByDescending(r => r.ReservationDate)
                .ThenByDescending(r => r.ReservationTime.TimeOfDay);

            // ⭐️ KẾT THÚC THAY ĐỔI LOGIC SẮP XẾP ⭐️

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .ToListAsync();

            return new PagedList<Reservation>(items, totalCount, parameters.PageNumber, parameters.PageSize);
        }

        // (2) Lấy chi tiết (Thay đổi: Guid -> int)
        public async Task<Reservation?> GetReservationDetailByIdAsync(int reservationId)
        {
            return await _context.Reservations
                .Include(r => r.Customer)
                    .ThenInclude(c => c.User)
                .Include(r => r.ReservationTables)
                    .ThenInclude(rt => rt.Table)
                        .ThenInclude(t => t.Area)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId); // Thay đổi
        }

        // (3) Lấy để Update (Thay đổi: Guid -> int)
        public async Task<Reservation?> GetReservationForUpdateAsync(int reservationId)
        {
            return await _context.Reservations
                .Include(r => r.ReservationTables)
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId); // Thay đổi
        }

        public void Update(Reservation reservation)
        {
            _context.Entry(reservation).State = EntityState.Modified;
        }

        // 1. Lấy thông tin bàn (bao gồm Area)
        public async Task<Table> GetTableInfoAsync(int tableId)
        {
            return await _context.Tables.AsNoTracking()
                .Include(t => t.Area) // Lấy Vị trí (Area)
                .FirstOrDefaultAsync(t => t.TableId == tableId);
        }

        // 2. Lấy Reservation đang active cho bàn đó
        // File: Repositories/OrderRepository.cs

        public async Task<Reservation> GetActiveReservationForTableAsync(int tableId)
        {
            // Bắt đầu từ ReservationTables
            var reservation = await _context.ReservationTables
                .AsNoTracking() // Thêm AsNoTracking vì đây là query đọc
                .Where(rt => rt.TableId == tableId)

                // --- SỬA LỖI: Include MỌI THỨ TRƯỚC ---
                // 1. Include 'Reservation'
                .Include(rt => rt.Reservation)
                    // 2. Từ 'Reservation', ThenInclude 'Customer' -> 'User'
                    .ThenInclude(r => r.Customer)
                        .ThenInclude(c => c.User)
                .Include(rt => rt.Reservation)
                    // 3. Từ 'Reservation', ThenInclude 'Orders' -> 'OrderDetails' -> 'MenuItem'
                    .ThenInclude(r => r.Orders)
                        .ThenInclude(o => o.OrderDetails)
                            .ThenInclude(od => od.MenuItem)
                .Include(rt => rt.Reservation)
                    // 4. Từ 'Reservation', ThenInclude 'Orders' -> 'OrderDetails' -> 'Combo'
                    .ThenInclude(r => r.Orders)
                        .ThenInclude(o => o.OrderDetails)
                            .ThenInclude(od => od.Combo)

                // 5. SAU KHI Include, mới Select
                .Select(rt => rt.Reservation)
                // --- HẾT SỬA LỖI ---

                // 6. Lọc trạng thái trên Reservation
                .Where(r => r.Status == "Confirmed" || r.Status == "Guest Seated")
                .FirstOrDefaultAsync();

            return reservation;
        }

        // 3. Lấy toàn bộ Menu
        // Repositories/OrderRepository.cs
        public async Task<List<MenuItem>> GetActiveMenuItemsAsync()
        {
            return await _context.MenuItems.AsNoTracking()
                .Where(m => m.IsAvailable == true)
                .Include(m => m.Category) // <-- SỬA THÀNH 'Category'
                .ToListAsync();
        }

        // 4. Lấy toàn bộ Combo
        public async Task<List<Combo>> GetActiveCombosAsync()
        {
            return await _context.Combos.AsNoTracking()
                .Where(c => c.IsAvailable == true)
                .ToListAsync();
        }

        public async Task<IEnumerable<MenuCategory>> GetCategoriesAsync()
        {
            return await _context.MenuCategories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }
    }
}