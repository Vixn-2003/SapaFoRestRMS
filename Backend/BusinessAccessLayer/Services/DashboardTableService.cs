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
using static BusinessAccessLayer.Services.Interfaces.IDashboardTableService;
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
        private readonly IInventoryIngredientService _inventoryService;
        private readonly IKitchenDisplayService _kitchenDisplayService;

        // ⭐️ SỬA LỖI 1 & 2: Cập nhật Constructor
        public DashboardTableService(
            IDashboardTableRepository dashboardRepo,
            IOrderTableRepository orderTableRepo,
            IUnitOfWork unitOfWork,
            IHubContext<ReservationHub> hubContext,
            SapaFoRestRmsContext context,
            IInventoryIngredientService inventoryService,
            IKitchenDisplayService kitchenDisplayService
            )
        {
            _dashboardRepo = dashboardRepo;
            _orderTableRepo = orderTableRepo;
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
            _context = context;
            _inventoryService = inventoryService;
            _kitchenDisplayService = kitchenDisplayService;
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

                // Nếu không có đơn -> Available
                // Nếu có đơn nhưng chưa có giờ ngồi (ArrivalAt null) -> Reserved 
                // Nếu có đơn VÀ đã có giờ ngồi -> Active (để UI hiện màu cam + đồng hồ chạy)
                Status = (data.ActiveReservation == null)
                         ? "Available"
                         : (data.ActiveReservation.ArrivalAt != null ? "Active" : "Reserved"),

                GuestCount = data.ActiveReservation?.NumberOfGuests ?? 0,
                GuestSeatedTime = data.ActiveReservation?.ArrivalAt,

                // map thêm ReservationTime để hiển thị "Khách đến lúc..." ở trạng thái Reserved
                ReservationTime = data.ActiveReservation?.ReservationTime,
                // Logic: Nếu có ActiveReservation thì mới lấy tên, ngược lại là null
                CustomerName = data.ActiveReservation != null
            ? (data.ActiveReservation.Customer?.User?.FullName ?? data.ActiveReservation.CustomerNameReservation)
            : null,

                CustomerPhone = data.ActiveReservation?.Customer?.User?.Phone ?? null,

    //            GrandTotal = data.ActiveReservation == null
    //? 0
    //: data.ActiveReservation.Orders
    //    .SelectMany(o => o.OrderDetails)
    //    .Where(od => od.Status == "Cooking" || od.Status == "Ready" || od.Status == "Done" || od.Status == "Pending")
    //    .Sum(od => od.Quantity * od.UnitPrice),

                GrandTotal = data.ActiveReservation == null
    ? 0
    : data.ActiveReservation.Orders
        .SelectMany(o => o.OrderDetails)
        .Where(od => od.Status != "Cancelled")
        .Sum(od => (od.Quantity) * (od.UnitPrice) ),
                reservationId = data.ActiveReservation?.ReservationId,

            }).ToList();

            // 3. Lọc theo Status (Cập nhật logic lọc nếu cần)
            if (!string.IsNullOrEmpty(status))
            {
                // Nếu muốn tách biệt hoàn toàn thì giữ nguyên:
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


        // (1) Lấy danh sách 
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
                ReservationTime = reservation.ReservationTime.TimeOfDay,
                NumberOfGuests = reservation.NumberOfGuests,
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
        // Đổi signature từ Task sang Task<Reservation>
        public async Task<Reservation> SeatGuestAsync(int reservationId)
        {
            // 1. Lấy dữ liệu (Lưu ý: Repo cần Include ReservationTables để lấy được TableId sau này)
            var reservation = await _dashboardRepo.GetReservationForUpdateAsync(reservationId);

            if (reservation == null)
                throw new Exception("Reservation not found.");

            // Kiểm tra trạng thái (Giữ nguyên logic cũ của bạn)
            // Lưu ý: Nếu logic của bạn cho phép chuyển từ "Available" -> "Active" luôn thì bỏ check Confirmed
            if (reservation.Status != "Confirmed" && reservation.Status != "Available")
                // Tùy vào luồng nghiệp vụ, đoạn này bạn tự cân nhắc bỏ hay giữ
                throw new InvalidOperationException("Reservation status is invalid.");

            if (reservation.ReservationTables == null || !reservation.ReservationTables.Any())
                throw new InvalidOperationException("No tables are assigned.");

            // 2. Cập nhật thông tin
            var now = DateTime.Now;
            reservation.Status = "Guest Seated"; // Sửa thành "Active" để khớp với logic hiển thị màu cam ở Frontend
            reservation.ArrivalAt = now;
            reservation.StatusUpdatedAt = now;

            // 3. Lưu xuống DB
            _dashboardRepo.Update(reservation);
            await _unitOfWork.SaveChangesAsync();

            // 4. Bắn SignalR (Realtime cho các máy khác)
            await NotifyClientsOfUpdate(reservation);

            // ⭐️ QUAN TRỌNG: Trả về đối tượng Reservation đã update
            return reservation;
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

                var latestOrder = activeReservation.Orders?
                    .OrderByDescending(o => o.CreatedAt ?? DateTime.MinValue)
                    .FirstOrDefault();

                if (latestOrder != null)
                {
                    screenDto.ActiveOrderId = latestOrder.OrderId;
                    // ✅ Đưa trạng thái order hiện tại ra FE để dùng cho flow waiter/cashier
                    // Chuẩn hoá về lowercase để so sánh đơn giản ở frontend
                    screenDto.OrderStatus = latestOrder.Status?.ToLowerInvariant();

                    foreach (var od in latestOrder.OrderDetails)
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
                else
                {
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
            }

            // ✅ TÍNH TOÁN SỐ LƯỢNG MÓN THEO TRẠNG THÁI (Backend)
            screenDto.TotalQuantity = screenDto.OrderedItems.Sum(item => item.Quantity);
            
            foreach (var item in screenDto.OrderedItems)
            {
                var status = (item.Status ?? "").Trim();
                var statusLower = status.ToLower();
                
                // Đã phục vụ & đang nấu: Status = "Cooking", "Done", "Ready", "Served"
                var isReady = statusLower == "cooking" ||
                             statusLower == "done" ||
                             statusLower == "ready" ||
                             statusLower == "served" ||
                             statusLower == "đang chế biến" ||
                             statusLower == "đã xong" ||
                             statusLower == "sẵn sàng";
                
                // Chưa nấu: Status = "Pending"
                var isPending = statusLower == "pending" ||
                               statusLower == "đã gửi" ||
                               string.IsNullOrEmpty(status);
                
                // Món đã hủy: Status = "Cancelled", "Removed"
                var isCancelled = statusLower == "cancelled" ||
                                 statusLower == "hủy" ||
                                 statusLower == "removed";
                
                if (isReady)
                {
                    screenDto.QtyServedAndCooking += item.Quantity;
                }
                else if (isPending)
                {
                    screenDto.QtyNotCooked += item.Quantity;
                }
                else if (isCancelled)
                {
                    screenDto.QtyCancelled += item.Quantity;
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


        // Lấy danh sách món đã gọi của khách
        // Lấy danh sách OrderDetail hiện tại của bàn

        public async Task SaveOrderChangesAsync(SaveOrderRequest request)
        {
            // BƯỚC 1: TÌM RESERVATION
            var activeReservation = await _dashboardRepo.GetActiveReservationByTableIdAsync(request.TableId);
            if (activeReservation == null)
            {
                throw new Exception($"Bàn {request.TableId} chưa có khách check-in.");
            }

            // BƯỚC 2: TÌM HOẶC TẠO ORDER (VỎ HÓA ĐƠN)
            // Đây là bước sửa lỗi "Foreign Key": Phải có Order thì mới thêm OrderDetail được
            var currentOrder = await _dashboardRepo.GetOrderByReservationIdAsync(activeReservation.ReservationId);

            if (currentOrder == null)
            {
                // Nếu chưa có hóa đơn -> Tạo mới
                currentOrder = new Order
                {
                    ReservationId = activeReservation.ReservationId,
                    CreatedAt = DateTime.Now,
                    TotalAmount = 0,    // Tạm tính là 0
                    Status = "Pending",  // Trạng thái chờ,
                    OrderType = "Tại bàn"
                };

                await _dashboardRepo.AddOrderAsync(currentOrder);
                // Lưu ngay lập tức để DB sinh ra OrderId (VD: 501)
                await _dashboardRepo.SaveChangesAsync();
            }

            // BƯỚC 3: XỬ LÝ TỪNG MÓN ĂN
            foreach (var itemDto in request.Items)
            {
                switch (itemDto.Action)
                {
                    // --- CASE ADD: THÊM MÓN MỚI ---
                    case "Add":
                        Console.WriteLine("--- [DEBUG] BẮT ĐẦU CASE ADD ---");
                        decimal price = 0;

                        // 1. Lấy giá
                        if (itemDto.MenuItemId.HasValue)
                        {
                            var menu = await _dashboardRepo.GetMenuItemAsync(itemDto.MenuItemId.Value);
                            price = menu?.Price ?? 0;
                        }
                        else if (itemDto.ComboId.HasValue)
                        {
                            var combo = await _dashboardRepo.GetComboAsync(itemDto.ComboId.Value);
                            price = (decimal)(combo?.Price ?? 0);
                            Console.WriteLine($"[DEBUG] Đang add Combo ID: {itemDto.ComboId.Value} - Giá: {price}");
                        }

                        // 2. Tạo OrderDetail
                        var newDetail = new OrderDetail
                        {
                            OrderId = currentOrder.OrderId,
                            MenuItemId = (itemDto.ComboId.HasValue && itemDto.ComboId > 0) ? null : itemDto.MenuItemId,
                            ComboId = (itemDto.ComboId.HasValue && itemDto.ComboId > 0) ? itemDto.ComboId : null,
                            Quantity = itemDto.Quantity,
                            UnitPrice = price,
                            Notes = itemDto.Note,
                            Status = "Pending",
                            CreatedAt = DateTime.Now
                        };

                        await _dashboardRepo.AddOrderDetailAsync(newDetail);
                        await _dashboardRepo.SaveChangesAsync();

                        Console.WriteLine($"[DEBUG] Đã lưu OrderDetail. ID mới sinh ra là: {newDetail.OrderDetailId}");

                        // 3. LOGIC INSERT ORDER COMBO ITEMS
                        if (newDetail.ComboId.HasValue)
                        {
                            Console.WriteLine($"[DEBUG] Phát hiện đây là Combo (ID: {newDetail.ComboId}). Bắt đầu tìm món con...");

                            // Lấy danh sách món con
                            var comboComponents = await _dashboardRepo.GetComboItemsByComboIdAsync(newDetail.ComboId.Value);

                            // KIỂM TRA QUAN TRỌNG
                            if (comboComponents == null || !comboComponents.Any())
                            {
                                Console.WriteLine($"[DEBUG] ❌ CẢNH BÁO: Không tìm thấy món con nào trong bảng ComboItems cho ComboId = {newDetail.ComboId.Value}. Vui lòng kiểm tra Database bảng ComboItems!");
                            }
                            else
                            {
                                Console.WriteLine($"[DEBUG] ✅ Tìm thấy {comboComponents.Count} món con. Bắt đầu Insert...");

                                foreach (var component in comboComponents)
                                {
                                    var orderComboItem = new OrderComboItem
                                    {
                                        OrderDetailId = newDetail.OrderDetailId,
                                        MenuItemId = component.MenuItemId,
                                        Quantity = component.Quantity * newDetail.Quantity,
                                        Status = "Pending",
                                        CreatedAt = DateTime.Now,
                                        Notes = newDetail.Notes,
                                        IsUrgent = false
                                    };

                                    await _dashboardRepo.AddOrderComboItemAsync(orderComboItem);
                                    Console.WriteLine($"[DEBUG] -> Đã Add vào Context món: {component.MenuItemId}");
                                }

                                // Save lần 2
                                await _dashboardRepo.SaveChangesAsync();
                                Console.WriteLine("[DEBUG] ✅ Đã gọi SaveChangesAsync() cho OrderComboItems.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("[DEBUG] Đây không phải là Combo, bỏ qua bước tách món.");
                        }



                        var reserveResult = await _inventoryService.ReserveBatchesForOrderDetailAsync(newDetail.OrderDetailId);
                        if (!reserveResult.success)
                        {
                            // Log warning nhưng không fail
                            Console.WriteLine($"Warning: Không thể reserve nguyên liệu cho OrderDetail {newDetail.OrderDetailId}: {reserveResult.message}");
                        }

                        // ✅ Broadcast đơn mới đến màn hình bếp qua SignalR
                        try
                        {
                            await NotifyKitchenNewOrderAsync(currentOrder.OrderId);
                        }
                        catch (Exception ex)
                        {
                            // Log error nhưng không fail việc thêm đơn
                            Console.WriteLine($"Warning: Không thể broadcast đơn mới đến bếp: {ex.Message}");
                        }

                        break;

                    case "Update":
                        // Tìm món trong DB theo ID gửi lên
                        var existingItem = await _dashboardRepo.GetOrderDetailByIdAsync(itemDto.OrderItemId);

                        // Kiểm tra: Có món này + Thuộc đúng hóa đơn này + Chưa bị hủy/thanh toán
                        if (existingItem != null && existingItem.Order.ReservationId == activeReservation.ReservationId)
                        {
                            // Kiểm tra trạng thái (Đảm bảo khớp với DB của bạn: "Cancelled" hay "Đã hủy")
                            if (existingItem.Status != "Đã hủy" && existingItem.Status != "Cancelled" && existingItem.Status != "Paid")
                            {
                                // 1. Cập nhật giá trị mới
                                existingItem.Quantity = itemDto.Quantity;
                                existingItem.Notes = itemDto.Note;

                                // 2. GỌI HÀM UPDATE REPO (QUAN TRỌNG)
                                await _dashboardRepo.UpdateOrderDetailAsync(existingItem);

                                if (existingItem.ComboId.HasValue)
                                {
                                    // A. Lấy danh sách món con hiện tại trong Order này
                                    var currentChildItems = await _dashboardRepo.GetOrderComboItemsByOrderDetailIdAsync(existingItem.OrderDetailId);

                                    // B. Lấy công thức chuẩn của Combo (để biết định lượng 1 combo có bao nhiêu món con)
                                    var comboDefinitions = await _dashboardRepo.GetComboItemsByComboIdAsync(existingItem.ComboId.Value);

                                    if (currentChildItems != null && comboDefinitions != null)
                                    {
                                        foreach (var childItem in currentChildItems)
                                        {
                                            // Tìm xem món con này tương ứng với dòng định nghĩa nào (để lấy định lượng gốc)
                                            var definition = comboDefinitions.FirstOrDefault(x => x.MenuItemId == childItem.MenuItemId);

                                            if (definition != null)
                                            {
                                                // C. Tính lại số lượng:
                                                // Số lượng con = (Định lượng gốc trong menu) * (Số lượng cha mới)
                                                // Ví dụ: 1 Combo có 2 gà. Khách sửa thành 3 Combo -> Con = 2 * 3 = 6 gà.
                                                childItem.Quantity = definition.Quantity * itemDto.Quantity;

                                                // Cập nhật ghi chú nếu cần (đồng bộ với cha)
                                                childItem.Notes = itemDto.Note;
                                            }
                                        }

                                        // D. Lưu thay đổi của các món con xuống DB
                                        // Vì các childItem đã được tracking bởi EF Core khi Query lên, 
                                        // nên chỉ cần gọi SaveChangesAsync là đủ.
                                        await _dashboardRepo.SaveChangesAsync();
                                    }
                                }
                            }
                        }
                        break;

                    case "Delete":
                        var itemToDelete = await _dashboardRepo.GetOrderDetailByIdAsync(itemDto.OrderItemId);

                        if (itemToDelete != null && itemToDelete.Order.ReservationId == activeReservation.ReservationId)
                        {
                            // ✅ QUAN TRỌNG: Giải phóng reserved quantity TRƯỚC KHI cập nhật status
                            // Nếu món đã được reserve nguyên liệu, cần giải phóng để available có thể tăng lại
                            // Phải gọi TRƯỚC khi set status = Cancelled để release có thể check status Pending/Cooking
                            if (itemToDelete.MenuItem != null)
                            {
                                var releaseResult = await _inventoryService.ReleaseReservedBatchesForOrderDetailAsync(itemToDelete.OrderDetailId);
                                if (!releaseResult.success)
                                {
                                    // Log warning nhưng không fail việc hủy món
                                    Console.WriteLine($"Warning: Không thể giải phóng nguyên liệu khi hủy món {itemToDelete.OrderDetailId}: {releaseResult.message}");
                                }
                            }

                            // Soft Delete: Đổi trạng thái (SAU KHI đã release)
                            itemToDelete.Status = "Cancelled"; // Hoặc "Cancelled" tùy DB

                            // GỌI HÀM UPDATE REPO
                            await _dashboardRepo.UpdateOrderDetailAsync(itemToDelete);
                        }
                        break;
                }
            }


            // BƯỚC 4: LƯU CÁC THAY ĐỔI CỦA MÓN ĂN
            await _dashboardRepo.SaveChangesAsync();
        }

        /// <summary>
        /// Broadcast đơn mới đến màn hình bếp qua SignalR
        /// </summary>
        private async Task NotifyKitchenNewOrderAsync(int orderId)
        {
            try
            {
                // Lấy order mới từ KitchenDisplayService
                var activeOrders = await _kitchenDisplayService.GetActiveOrdersAsync();
                var newOrder = activeOrders.FirstOrDefault(o => o.OrderId == orderId);

                if (newOrder != null)
                {
                    // Gọi method broadcast trong KitchenDisplayService
                    // Method này sẽ được implement trong KitchenDisplayService với IHubContext<KitchenHub>
                    await _kitchenDisplayService.NotifyNewOrderAddedAsync(newOrder);
                }
            }
            catch (Exception ex)
            {
                // Log error nhưng không throw để không ảnh hưởng đến flow chính
                Console.WriteLine($"Error notifying kitchen of new order {orderId}: {ex.Message}");
            }
        }
    }


}
