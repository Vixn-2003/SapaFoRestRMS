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
            // 1. Khởi tạo Query, Include thêm thông tin Khách hàng (Customer -> User) để tìm kiếm
            //var query = _context.Tables
            //    .Include(t => t.Area)
            //    .Include(t => t.ReservationTables)
            //        .ThenInclude(rt => rt.Reservation)
            //            .ThenInclude(r => r.Customer) // Include thêm Customer
            //                .ThenInclude(c => c.User) // Include thêm User để lấy FullName/Phone
            //    .AsQueryable();

            var query = _context.Tables
    .Include(t => t.Area)
    .Include(t => t.ReservationTables)
        .ThenInclude(rt => rt.Reservation)
            .ThenInclude(r => r.Customer)
                .ThenInclude(c => c.User)
    .Include(t => t.ReservationTables)
        .ThenInclude(rt => rt.Reservation)
            .ThenInclude(r => r.Orders)                  // ⭐ THÊM
                .ThenInclude(o => o.OrderDetails)        // ⭐ THÊM
    .AsQueryable();


            // 2. Lọc theo Tầng & Khu vực (Giữ nguyên)
            if (floor.HasValue)
            {
                query = query.Where(t => t.Area.Floor == floor.Value);
            }
            if (!string.IsNullOrEmpty(areaName))
            {
                query = query.Where(t => t.Area.AreaName == areaName);
            }

            // 3. ⭐️ LOGIC TÌM KIẾM NÂNG CAO (SỐ BÀN HOẶC TÊN/SĐT KHÁCH) ⭐️
            if (!string.IsNullOrEmpty(searchString))
            {
                // Tìm các bàn thỏa mãn 1 trong 2 điều kiện:
                // A. Số bàn chứa từ khóa
                // B. HOẶC Bàn đang có đơn (Active/Confirmed) mà Tên/SĐT khách chứa từ khóa
                query = query.Where(t =>
                    t.TableNumber.Contains(searchString)
                    ||
                    t.ReservationTables.Any(rt =>
                        // Chỉ tìm trong các đơn đang diễn ra (Active)
                        (rt.Reservation.Status == "Guest Seated" || rt.Reservation.Status == "Confirmed")
                        &&
                        (
                            // Tìm theo tên khách vãng lai (lưu trực tiếp trong Reservation)
                            (rt.Reservation.CustomerNameReservation != null && rt.Reservation.CustomerNameReservation.Contains(searchString))
                            ||
                            // Tìm theo SĐT khách vãng lai
                            (rt.Reservation.Customer.User.Phone != null && rt.Reservation.Customer.User.Phone.Contains(searchString))
                            ||
                            // Tìm theo Tên tài khoản đăng ký (nếu có)
                            (rt.Reservation.Customer != null && rt.Reservation.Customer.User.FullName.Contains(searchString))
                            ||
                            // Tìm theo SĐT tài khoản đăng ký (nếu có)
                            (rt.Reservation.Customer != null && rt.Reservation.Customer.User.Phone.Contains(searchString))
                        )
                    )
                );
            }

            // 4. Projection (Chọn kết quả & ActiveReservation)
            var projectedResult = await query
                .OrderBy(t => t.TableNumber)
                .Select(t => new {
                    Table = t,
                    // Lấy đơn đặt bàn "Active" phù hợp nhất để hiển thị thông tin lên thẻ
                    ActiveReservation = t.ReservationTables
                        .Select(rt => rt.Reservation)
                        .Where(r => r.Status == "Guest Seated" || r.Status == "Confirmed")
                        // Ưu tiên lấy đơn khớp với từ khóa tìm kiếm (nếu có) để hiển thị đúng người cần tìm
                        .OrderByDescending(r => !string.IsNullOrEmpty(searchString) && (
                             (r.CustomerNameReservation.Contains(searchString)) ||
                             (r.Customer.User.Phone.Contains(searchString))
                        ))
                        .ThenByDescending(r => r.ReservationTime)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var result = projectedResult.Select(data =>
                (data.Table, data.ActiveReservation)
            ).ToList();

            return result;
        }

        // (1) Lấy danh sách đơn đã đặt bàn
        public async Task<PagedList<Reservation>> GetPagedReservationsAsync(ReservationQueryParameters parameters)
        {
            var query = _context.Reservations
                .Include(r => r.Customer).ThenInclude(c => c.User)
                .Include(r => r.ReservationTables).ThenInclude(rt => rt.Table).ThenInclude(t => t.Area)
                .AsQueryable();

            // 🔥 1. Chỉ lấy đơn của NGÀY HÔM NAY
            var today = DateTime.Today;
            query = query.Where(r => r.ReservationDate.Date == today);

            // 2. Lọc theo TimeSlot (nếu có)
            if (!string.IsNullOrEmpty(parameters.TimeSlot))
            {
                query = query.Where(r => r.TimeSlot == parameters.TimeSlot);
            }

            // 3. Lọc theo Status
            bool isFilteringAll = string.IsNullOrEmpty(parameters.Status) || parameters.Status.ToLower() == "all";

            if (isFilteringAll)
            {
                query = query.Where(r => r.Status != "Pending" && r.Status != "Success");
            }
            else
            {
                query = query.Where(r => r.Status == parameters.Status);
            }

            // 4. Search
            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var searchTerm = parameters.SearchTerm.ToLower();

                query = query.Where(r =>
                    (r.Customer != null && r.Customer.User != null &&
                        (r.Customer.User.FullName.ToLower().Contains(searchTerm) ||
                         r.Customer.User.Phone.Contains(searchTerm))) ||
                    (r.CustomerNameReservation != null && r.CustomerNameReservation.ToLower().Contains(searchTerm))
                );
            }

            // 5. Sắp xếp
            var now = DateTime.Now.TimeOfDay;

            query = query
                // Ưu tiên status (nếu cần giữ logic cũ)
                .OrderBy(r => r.Status == "Confirmed" ? 0 :
                              r.Status == "Guest Seated" ? 1 : 2)

                // 🔥 Đưa đơn chưa đến giờ lên đầu, trễ giờ xuống dưới
                .ThenBy(r => r.ReservationTime.TimeOfDay < now ? 1 : 0)

                // Sắp theo giờ tăng dần
                .ThenBy(r => r.ReservationTime.TimeOfDay)

                // Giữ lại phần sắp xếp phụ
                .ThenByDescending(r => r.ArrivalAt);

            // 6. Phân trang
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
                        .ThenInclude(o => o.OrderDetails).
                           ThenInclude(t=>t.OrderComboItems)
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

        ///////////////////////
        ///
        public async Task<Reservation?> GetActiveReservationByTableIdAsync(int tableId)
        {
            // Lấy đơn có trạng thái Đang ngồi / Đã xác nhận / Active
            return await _context.ReservationTables
                .Include(rt => rt.Reservation)
                .Where(rt => rt.TableId == tableId &&
                            (rt.Reservation.Status == "Guest Seated" ||
                             rt.Reservation.Status == "Confirmed" ||
                             rt.Reservation.Status == "Active"))
                .Select(rt => rt.Reservation)
                .FirstOrDefaultAsync();
        }

        public async Task<Order?> GetOrderByReservationIdAsync(int reservationId)
        {
            // Tìm Order chưa thanh toán (Paid) của Reservation này
            return await _context.Orders
                .FirstOrDefaultAsync(o => o.ReservationId == reservationId && o.Status != "Paid");
        }

        public async Task AddOrderAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
        }

        public async Task<MenuItem?> GetMenuItemAsync(int id)
        {
            return await _context.MenuItems.FindAsync(id);
        }

        public async Task<Combo?> GetComboAsync(int id)
        {
            return await _context.Combos.FindAsync(id);
        }

        public async Task<OrderDetail?> GetOrderDetailByIdAsync(int orderDetailId)
        {
            return await _context.OrderDetails
                .Include(od => od.Order)                 
                .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);
        }

        public async Task AddOrderDetailAsync(OrderDetail item)
        {
            await _context.OrderDetails.AddAsync(item);
        }
        // Thêm hàm này vào Class Repository
        public async Task UpdateOrderDetailAsync(OrderDetail item)
        {
            _context.OrderDetails.Update(item); // Báo cho EF biết dòng này đã thay đổi
                                                // Lưu ý: Không cần await SaveChanges ở đây vì Service sẽ gọi SaveChanges cuối cùng
            await Task.CompletedTask;
        }
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }


        public async Task<List<ComboItem>> GetComboItemsByComboIdAsync(int comboId)
        {
            // Truy vấn bảng ComboItems, lọc theo ComboId
            var items = await _context.ComboItems
                                      .Where(x => x.ComboId == comboId)
                                      .ToListAsync();
            return items;
        }

        // Hàm 2: Thêm mới một dòng vào bảng OrderComboItems
        public async Task AddOrderComboItemAsync(OrderComboItem item)
        {
            // Chỉ thêm vào context, chưa SaveChanges (vì SaveChanges gọi ở Service để đồng bộ)
            await _context.OrderComboItems.AddAsync(item);
        }
        public async Task<List<OrderComboItem>> GetOrderComboItemsByOrderDetailIdAsync(int orderDetailId)
        {
            return await _context.OrderComboItems
                                 .Where(x => x.OrderDetailId == orderDetailId)
                                 .ToListAsync();
        }
    }
}