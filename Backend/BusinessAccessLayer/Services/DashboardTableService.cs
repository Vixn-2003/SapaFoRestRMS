using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.DTOs.OrderGuest;
using BusinessAccessLayer.DTOs.OrderGuest.ListOrder; 
using BusinessAccessLayer.Hubs;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Common;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using static BusinessAccessLayer.Services.OrderTableService;
using ComboDto = BusinessAccessLayer.DTOs.OrderGuest.ComboDto;

namespace BusinessAccessLayer.Services
{
    public class DashboardTableService : IDashboardTableService
    {
        private readonly IDashboardTableRepository _dashboardRepo;
        private readonly IOrderTableRepository _orderTableRepo;
        private readonly IUnitOfWork _unitOfWork; 
        private readonly IHubContext<ReservationHub> _hubContext;
        private readonly SapaFoRestRmsContext _context; // Cần DbContext để Save

        // ⭐️ SỬA LỖI 1 & 2: Cập nhật Constructor
        public DashboardTableService(
            IDashboardTableRepository dashboardRepo,
            IOrderTableRepository orderTableRepo,
            IUnitOfWork unitOfWork,
            IHubContext<ReservationHub> hubContext,
            SapaFoRestRmsContext context
            )
        {
            _dashboardRepo = dashboardRepo;
            _orderTableRepo = orderTableRepo;
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
            _context = context;
        }

        public async Task<DashboardDataDto> GetDashboardDataAsync(string? areaName, int? floor, string? status, string? searchString, int page, int pageSize)
        {
            var dashboardData = new DashboardDataDto();

            // 1. Gọi Repo
            var allTablesWithStatus = await _dashboardRepo.GetFilteredTablesWithStatusAsync(areaName, floor, searchString);

            // 2. Chuyển đổi (Map) sang DTO
            var allTableDtos = allTablesWithStatus.Select(data => new TableDashboardDto
            {
                TableId = data.Table.TableId,
                TableNumber = data.Table.TableNumber,
                AreaName = data.Table.Area.AreaName,
                Floor = data.Table.Area.Floor,
                Capacity = data.Table.Capacity,
                Status = (data.ActiveReservation != null) ? "Active" : "Available",
                GuestCount = data.ActiveReservation?.NumberOfGuests ?? 0,

                // ⭐️ SỬA LỖI LOGIC 4: Phải lấy 'ArrivalAt' (giờ khách ngồi) thay vì 'ReservationTime' (giờ đặt)
                GuestSeatedTime = data.ActiveReservation?.ArrivalAt
            }).ToList();

            // 3. Lọc theo Status
            if (!string.IsNullOrEmpty(status))
            {
                allTableDtos = allTableDtos.Where(t => t.Status == status).ToList();
            }

            // 4. Lấy tổng số lượng
            dashboardData.TotalCount = allTableDtos.Count;

            // 5. Phân trang
            dashboardData.Tables = allTableDtos
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 6. Lấy dữ liệu cho bộ lọc
            dashboardData.AreaNames = await _orderTableRepo.GetDistinctAreaNamesAsync();
            dashboardData.Floors = await _orderTableRepo.GetDistinctFloorsAsync();

            return dashboardData;
        }


        // (1) Lấy danh sách - MAP THỦ CÔNG
        public async Task<PagedList<ReservationListDto>> GetReservationsAsync(ReservationQueryParameters parameters)
        {
            var pagedReservations = await _dashboardRepo.GetPagedReservationsAsync(parameters);

            var dtoList = new List<ReservationListDto>();
            foreach (var reservation in pagedReservations.Items)
            {
                dtoList.Add(new ReservationListDto
                {
                    ReservationId = reservation.ReservationId,
                    CustomerName = reservation.Customer?.User?.FullName ?? reservation.CustomerNameReservation,
                    CustomerPhone = reservation.Customer?.User?.Phone,
                    Areas = string.Join(", ", reservation.ReservationTables
                                            .Select(rt => rt.Table.Area.AreaName)
                                            .Distinct()),
                    Tables = string.Join(", ", reservation.ReservationTables
                                            .Select(rt => rt.Table.TableNumber)),

                    // ⭐️ SỬA LỖI 3: Chuyển đổi DateTime -> TimeSpan
                    ReservationTime = reservation.ReservationTime.TimeOfDay,
                    TimeSlot = reservation.TimeSlot,
                    Status = reservation.Status,
                    ArrivalAt = reservation.ArrivalAt
                });
            }

            return new PagedList<ReservationListDto>(
                dtoList,
                pagedReservations.TotalCount,
                pagedReservations.PageNumber,
                pagedReservations.PageSize
            );
        }

        // (2) Lấy chi tiết - MAP THỦ CÔNG
        public async Task<ReservationDetailDto> GetReservationDetailAsync(int reservationId)
        {
            var reservation = await _dashboardRepo.GetReservationDetailByIdAsync(reservationId);

            if (reservation == null)
            {
                throw new Exception("Reservation not found.");
            }

            var detailDto = new ReservationDetailDto
            {
                ReservationId = reservation.ReservationId,
                Status = reservation.Status,
                Notes = reservation.Notes,
                CustomerId = reservation.CustomerId,
                CustomerName = reservation.Customer?.User?.FullName ?? reservation.CustomerNameReservation,
                CustomerPhone = reservation.Customer?.User?.Phone,
                CustomerEmail = reservation.Customer?.User?.Email,
                ReservationDate = reservation.ReservationDate,
                TimeSlot = reservation.TimeSlot,

                // ⭐️ SỬA LỖI 3: Chuyển đổi DateTime -> TimeSpan
                ReservationTime = reservation.ReservationTime.TimeOfDay,
                NumberOfGuests = reservation.NumberOfGuests,

                // ⭐️ SỬA LỖI 4: Chuyển đổi decimal? -> decimal
                DepositAmount = reservation.DepositAmount ?? 0m,
                DepositPaid = reservation.DepositPaid,

                AssignedTables = reservation.ReservationTables.Select(rt => new TableDetailDto
                {
                    TableId = rt.TableId,
                    TableNumber = rt.Table.TableNumber,
                    Capacity = rt.Table.Capacity,
                    AreaName = rt.Table.Area.AreaName,
                    Floor = rt.Table.Area.Floor
                }).ToList()
            };

            return detailDto;
        }

        // (3) Đổi trạng thái
        public async Task SeatGuestAsync(int reservationId)
        {
            var reservation = await _dashboardRepo.GetReservationForUpdateAsync(reservationId);

            if (reservation == null)
                throw new Exception("Reservation not found.");
            if (reservation.Status != "Confirmed")
                throw new InvalidOperationException("Reservation status must be 'Confirmed'.");
            if (reservation.ReservationTables == null || !reservation.ReservationTables.Any())
                throw new InvalidOperationException("No tables are assigned.");

            var now = DateTime.Now;
            reservation.Status = "Guest Seated";
            reservation.ArrivalAt = now;
            reservation.StatusUpdatedAt = now;

            _dashboardRepo.Update(reservation);

            // ⭐️ SỬA LỖI 1: Giờ _unitOfWork đã tồn tại
            await _unitOfWork.SaveChangesAsync();

            // ⭐️ SỬA LỖI 2: Giờ _hubContext đã tồn tại
            await NotifyClientsOfUpdate(reservation);
        }

        // Hàm SignalR
        private async Task NotifyClientsOfUpdate(Reservation reservation)
        {
            // ⭐️ SỬA LỖI 2: Giờ _hubContext đã tồn tại
            await _hubContext.Clients.All.SendAsync("ReservationStatusChanged", new
            {
                reservationId = reservation.ReservationId,
                newStatus = reservation.Status,
                arrivalAt = reservation.ArrivalAt
            });

            var tableIds = reservation.ReservationTables.Select(rt => rt.TableId);
            await _hubContext.Clients.All.SendAsync("TableStatusUpdated", new
            {
                tableIds = tableIds,
                status = "Occupied",
                reservationId = reservation.ReservationId,
                arrivalAt = reservation.ArrivalAt
            });
        }


        // --- ĐÂY LÀ HÀM QUAN TRỌNG ĐÃ SỬA ---
        public async Task<StaffOrderScreenDto> GetStaffOrderScreenAsync(int tableId, int? categoryId, string? searchString)
        {
            if (tableId <= 0)
                throw new ArgumentException("Table ID không hợp lệ.");

            // 1. Lấy thông tin Bàn
            var table = await _dashboardRepo.GetTableInfoAsync(tableId);
            if (table == null)
                throw new Exception("Không tìm thấy bàn.");

            // 2. Lấy Reservation (Giữ nguyên logic Include để hiển thị tên món đã gọi)
            var activeReservation = await _context.Reservations
                  .Include(r => r.Customer).ThenInclude(c => c.User)
                  .Include(r => r.Orders)
                      .ThenInclude(o => o.OrderDetails)
                          .ThenInclude(od => od.MenuItem)
                  .Include(r => r.Orders)
                      .ThenInclude(o => o.OrderDetails)
                          .ThenInclude(od => od.Combo)
                  .Where(r => r.ReservationTables.Any(rt => rt.TableId == tableId)
                           && r.Status == "Guest Seated")
                  .FirstOrDefaultAsync();

            // 3. CHUẨN BỊ DỮ LIỆU MENU & COMBO
            IEnumerable<MenuItem> menuItems = new List<MenuItem>();
            IEnumerable<Combo> combos = new List<Combo>();
            string searchLower = searchString?.ToLower().Trim();

            // --- XỬ LÝ QUERY COMBO (MỚI: INCLUDE ĐỂ TÍNH GIÁ GỐC) ---
            // Ta tạo query cơ bản có Include sẵn để dùng cho các trường hợp bên dưới
            var baseComboQuery = _context.Combos
                .Include(c => c.ComboItems)           // <-- QUAN TRỌNG: Để lấy danh sách món trong combo
                    .ThenInclude(ci => ci.MenuItem)   // <-- QUAN TRỌNG: Để lấy giá gốc của từng món
                .Where(c => c.IsAvailable == true)
                .AsQueryable(); // Để tiếp tục nối chuỗi query

            // --- TRƯỜNG HỢP 1: Chỉ lấy Combos (CategoryId = -1) ---
            if (categoryId.HasValue && categoryId.Value == -1)
            {
                if (!string.IsNullOrEmpty(searchLower))
                {
                    baseComboQuery = baseComboQuery.Where(c => c.Name.ToLower().Contains(searchLower));
                }
                combos = await baseComboQuery.ToListAsync();
            }
            // --- TRƯỜNG HỢP 2: Lấy Tất cả (CategoryId = null hoặc 0) ---
            else if (!categoryId.HasValue || categoryId.Value == 0)
            {
                // a. Lấy MenuItems
                var menuQuery = await _dashboardRepo.GetActiveMenuItemsAsync();
                // b. Lấy Combos (dùng query có Include ở trên)

                if (!string.IsNullOrEmpty(searchLower))
                {
                    menuQuery = menuQuery.Where(m => m.Name.ToLower().Contains(searchLower)).ToList();
                    baseComboQuery = baseComboQuery.Where(c => c.Name.ToLower().Contains(searchLower));
                }

                menuItems = menuQuery;
                combos = await baseComboQuery.ToListAsync();
            }
            // --- TRƯỜNG HỢP 3: Lấy Category cụ thể ---
            else
            {
                var menuQuery = await _dashboardRepo.GetActiveMenuItemsAsync();
                menuQuery = menuQuery.Where(m => m.CategoryId == categoryId.Value).ToList();

                if (!string.IsNullOrEmpty(searchLower))
                {
                    menuQuery = menuQuery.Where(m => m.Name.ToLower().Contains(searchLower)).ToList();
                }
                menuItems = menuQuery;
                // Combos rỗng
            }

            // 4. MAPPING SANG DTO
            var screenDto = new StaffOrderScreenDto();

            // Map Bàn
            screenDto.TableId = table.TableId;
            screenDto.TableNumber = table.TableNumber;
            screenDto.AreaName = table.Area?.AreaName;
            screenDto.Floor = table.Area?.Floor ?? 0;

            // Map MenuItems
            screenDto.MenuItems = menuItems.Select(m => new DTOs.OrderGuest.MenuItemDto
            {
                MenuItemId = m.MenuItemId,
                Name = m.Name,
                CategoryName = m.Category?.CategoryName,
                Price = m.Price,
                ImageUrl = m.ImageUrl,
                IsAvailable = m.IsAvailable
            }).ToList();

            // Map Combos (CÓ TÍNH TOÁN ORIGINAL PRICE)
            screenDto.Combos = combos.Select(c => new ComboDto
            {
                ComboId = c.ComboId,
                Name = c.Name,
                ImageUrl = c.ImageUrl,
                IsAvailable = c.IsAvailable,

                Price = c.Price, // Giá bán (giá ưu đãi)

                // === LOGIC TÍNH TOÁN GIÁ GỐC (Giống hàm BuildComboDtoAsync) ===
                OriginalPrice = c.ComboItems.Sum(ci =>
                    (ci.MenuItem != null ? ci.MenuItem.Price * ci.Quantity : 0)
                )
            }).ToList();

            // Map Order (Phần bên phải - Giữ nguyên)
            if (activeReservation != null)
            {
                screenDto.ReservationId = activeReservation.ReservationId;
                screenDto.GuestCount = activeReservation.NumberOfGuests;

                if (activeReservation.Customer?.User != null)
                {
                    screenDto.CustomerName = activeReservation.Customer.User.FullName;
                    screenDto.CustomerPhone = activeReservation.Customer.User.Phone;
                }

                foreach (var order in activeReservation.Orders)
                {
                    foreach (var od in order.OrderDetails)
                    {
                        string itemName = od.MenuItemId.HasValue
                                          ? od.MenuItem?.Name
                                          : (od.ComboId.HasValue ? od.Combo?.Name : "Lỗi dữ liệu");

                        if (itemName == null) continue;

                        screenDto.OrderedItems.Add(new OrderedItemDto
                        {
                            OrderDetailId = od.OrderDetailId,
                            MenuItemId = od.MenuItemId,
                            ComboId = od.ComboId,
                            ItemName = itemName,
                            Quantity = od.Quantity,
                            UnitPrice = od.UnitPrice,
                            Status = od.Status,
                            Notes = od.Notes
                        });
                    }
                }
            }

            return screenDto;
        }
        // Trong Implementation
        public async Task<List<CategoryDto>> GetAllCategoriesAsync()
        {
            // Giả sử bạn có Repo lấy danh mục. Nếu chưa, dùng _context.Categories.ToListAsync()
            var categories = await _dashboardRepo.GetCategoriesAsync();

            var categoriesDto = categories.Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName
            }).ToList();

            // Thêm "Combos"
            categoriesDto.Add(new CategoryDto
            {
                CategoryId = -1, // <-- ID ảo đặc biệt
                CategoryName = "Combos"
            });

            return categoriesDto;
        }
    }
}