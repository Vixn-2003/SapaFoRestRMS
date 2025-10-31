using AutoMapper;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using QRCoder;
using System.ComponentModel.DataAnnotations;
namespace BusinessAccessLayer.Services
{
    public class OrderTableService : IOrderTableService
    {
        private readonly IOrderTableRepository _orderTableRepository;
        private readonly IConfiguration _config; //  KHAI BÁO _config
        private readonly SapaFoRestRmsContext _context; // Cần DbContext để Save
        public OrderTableService(
             IOrderTableRepository orderTableRepository,
             IConfiguration config, SapaFoRestRmsContext context)
        {
            _orderTableRepository = orderTableRepository;
            _config = config;
            _context = context;
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

        public async Task<IEnumerable<MenuItemDto>> GetMenuForReservationAsync(
    int reservationId,
    string status,
    int? categoryId,
    string? searchString)
        {
            // Kiểm tra reservation hợp lệ
            var reservation = await _orderTableRepository.GetReservationByIdAndStatusAsync(reservationId, status);
            if (reservation == null)
                throw new Exception("Reservation not found or invalid status");

            // Lấy danh sách món khả dụng 
            var menuItems = await _orderTableRepository.GetAvailableMenuWithCategoryAsync(categoryId, searchString);

            // Lấy danh sách món đã gọi (nếu có)
            var orderedItems = reservation.Orders
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => od.MenuItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            // Map sang DTO (Giữ nguyên)
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


        // Dựa vào danh sách đã hiện lấy các món ăn của hệ thông ra để lưu vào order sau đó sẽ gửi vào bếp .
        // Xử lí trạng thái bàn.
        // xử lí các bàn ghép với nhau
        public async Task<IEnumerable<TableQRDTO>> GetAllTablesAsync()
        {
            var tables = await _orderTableRepository.GetAllWithAreaAsync();

            // Ánh xạ thủ công từ List<Table> sang List<TableQRDTO>
            return tables.Select(table => new TableQRDTO
            {
                TableId = table.TableId,
                TableNumber = table.TableNumber,
                Capacity = table.Capacity,
                Status = table.Status,
                AreaId = table.AreaId,
                // Kiểm tra null cho Area để tránh lỗi
                AreaName = table.Area != null ? table.Area.AreaName : "N/A",
                Floor = table.Area.Floor,

            }).ToList();
        }

        public async Task<IEnumerable<TableQRDTO>> GetTablesAsync(
        int page, int pageSize,
        string? searchString, string? areaName, int? floor)
        {
            var query = _orderTableRepository.GetFilteredTables(searchString, areaName, floor);

            var tables = await query
                .OrderBy(t => t.TableNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return tables.Select(t => new TableQRDTO
            {
                TableId = t.TableId,
                TableNumber = t.TableNumber,
                Capacity = t.Capacity,
                Status = t.Status,
                AreaId = t.AreaId,
                AreaName = t.Area?.AreaName ?? "N/A",
                Floor = t.Area.Floor
            }).ToList();
        }

        public async Task<int> GetTotalCountAsync(string? searchString, string? areaName, int? floor)
        {
            var query = _orderTableRepository.GetFilteredTables(searchString, areaName, floor);
            return await query.CountAsync();
        }

        // HÀM GenerateQrCodeForTableAsync (Giữ nguyên, không thay đổi)
        public async Task<byte[]> GenerateQrCodeForTableAsync(int tableId)
        {
            var table = await _orderTableRepository.GetByTbIdAsync(tableId);
            if (table == null)
            {
                return null;
            }

            string ip = _config["WebApp:LocalIp"]; // Dùng indexer
            string port = _config["WebApp:Port"]; // Dùng indexer

            if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(port))
            {
                throw new Exception("Không tìm thấy cấu hình 'WebApp:LocalIp' hoặc 'WebApp:Port' trong appsettings.json");
            }

            string menuUrl = $"http://{ip}:{port}/MenuOrder?tableId={table.TableId}";

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(menuUrl, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeAsPngByteArr = qrCode.GetGraphic(20);

            return qrCodeAsPngByteArr;
        }

        // === HÀM MỚI CHO KHÁCH QUÉT QR ===
        // (Xóa hàm BuildMenuDtoForReservation cũ đi)

        public async Task<MenuPageViewModel> GetMenuForTableAsync(int tableId, int? categoryId, string? searchString)
        {
            // 1. Lấy chi tiết Bàn (như cũ)
            var table = await _orderTableRepository.GetByTbIdAsync(tableId);
            if (table == null)
            {
                throw new Exception("Bàn không tồn tại.");
            }

            // 2. Lấy Reservation (phải .Include(MenuItem) để lấy tên món)
            var reservation = await _context.Reservations
                .Include(r => r.Orders)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.MenuItem) // <-- Cần cho OrderedItems
                .Where(r => r.ReservationTables.Any(rt => rt.TableId == tableId) && r.Status == "Guest Seated")
                .FirstOrDefaultAsync();

            if (reservation == null)
            {
                throw new Exception("Bàn không hợp lệ hoặc hiện không có khách.");
            }

            // 3. Lấy danh sách món đã gọi (như cũ)
            var orderedItems = reservation.Orders
                .SelectMany(o => o.OrderDetails)
                .Where(od => od.Status != "Đã hủy")
                .Select(od => new OrderDetailStatusDto
                {
                    OrderDetailId = od.OrderDetailId,
                    MenuItemId = od.MenuItemId,
                    ItemName = od.MenuItem.Name, // Lấy tên từ MenuItem
                    Quantity = od.Quantity,
                    Status = od.Status,
                    CreatedAt = od.CreatedAt,
                    Notes = od.Notes
                })
                .OrderByDescending(od => od.CreatedAt)
                .ToList();

            // 4. GỌI HELPER: Lấy Menu đã được lọc/search
            var menuDto = await BuildMenuDtoForReservation(reservation, categoryId, searchString);

            // 5. Trả về (như cũ)
            return new MenuPageViewModel
            {
                MenuItems = menuDto,
                OrderedItems = orderedItems,
                TableNumber = table.TableNumber,
                AreaName = table.Area?.AreaName ?? "N/A",
                Floor = table.Area?.Floor
            };
        }

        // Lấy category
        public async Task<IEnumerable<MenuCategoryDto>> GetMenuCategoriesAsync()
        {
            var categories = await _orderTableRepository.GetAllCategoriesAsync();
            return categories.Select(c => new MenuCategoryDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName
            });
        }
        // (Hàm private helper trong OrderTableService.cs)

        // === SỬA HÀM NÀY ===
        private async Task<IEnumerable<MenuItemDto>> BuildMenuDtoForReservation(
     Reservation reservation,
     int? categoryId,      // <-- Thêm
     string? searchString) // <-- Thêm
        {
            // 1. Gọi Repository đã được lọc
            var menuItems = await _orderTableRepository.GetAvailableMenuWithCategoryAsync(categoryId, searchString);

            // 2. Code cũ của bạn giữ nguyên
            var orderedItems = reservation.Orders
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => od.MenuItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            return menuItems.Select(m => new MenuItemDto
            {
                MenuItemId = m.MenuItemId,
                Name = m.Name,
                CategoryName = m.Category?.CategoryName ?? "",
                Price = m.Price,
                ImageUrl = m.ImageUrl,
                IsAvailable = m.IsAvailable,
                Quantity = orderedItems.ContainsKey(m.MenuItemId) ? orderedItems[m.MenuItemId] : 0
            }).ToList();
        }
        public async Task<List<string>> GetAreaNamesAsync()
        {
            return await _orderTableRepository.GetDistinctAreaNamesAsync();
        }

        public async Task<List<int?>> GetFloorsAsync()
        {
            return await _orderTableRepository.GetDistinctFloorsAsync();
        }
        // === SỬA LẠI HÀM NÀY ===
        // Đổi kiểu trả về từ Task<Order> thành Task<OrderResultDto>
        public async Task<OrderResultDto> SubmitOrderAsync(OrderSubmissionDto orderDto)
        {
            // 1. Kiểm tra Reservation (An ninh nghiệp vụ)
            var reservation = await _orderTableRepository.GetActiveReservationByTableIdAsync(orderDto.TableId);
            if (reservation == null)
            {
                throw new Exception("Không tìm thấy bàn hợp lệ hoặc bàn chưa được kích hoạt.");
            }

            // 2. Lấy giá gốc từ DB (An ninh giá)
            var itemIds = orderDto.Items.Select(i => i.MenuItemId).ToList();
            var menuItemsFromDb = await _orderTableRepository.GetMenuItemsByIdsAsync(itemIds);
            var priceMap = menuItemsFromDb.ToDictionary(m => m.MenuItemId, m => m.Price);

            // 3. Tạo Order
            var newOrder = new Order
            {
                ReservationId = reservation.ReservationId,

                // === FIX 1: THÊM CustomerId ===
                CustomerId = reservation.CustomerId,

                CreatedAt = DateTime.UtcNow,
                OrderType = "Tại bàn",
                Status = "Pending",
                TotalAmount = 0,
                OrderDetails = new List<OrderDetail>()
            };

            // 4. Tạo OrderDetails
            foreach (var cartItem in orderDto.Items)
            {
                if (!priceMap.TryGetValue(cartItem.MenuItemId, out var price))
                {
                    continue;
                }

                var orderDetail = new OrderDetail
                {
                    MenuItemId = cartItem.MenuItemId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = price, // (Bạn đã sửa đúng)
                    Status = "Đã gửi",
                    CreatedAt = DateTime.UtcNow,
                    Notes = cartItem.Notes, // <-- Thêm dòng này

                };

                newOrder.OrderDetails.Add(orderDetail);
                newOrder.TotalAmount += (price * cartItem.Quantity);
            }

            if (newOrder.OrderDetails.Count == 0)
            {
                throw new Exception("Giỏ hàng trống hoặc các món đã chọn không hợp lệ.");
            }

            // 5. Lưu vào Database
            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            // === FIX 2: Trả về DTO để tránh lỗi JSON Cycle ===
            var result = new OrderResultDto
            {
                OrderId = newOrder.OrderId,
                Status = newOrder.Status,
                TotalAmount = newOrder.TotalAmount,
                CreatedAt = newOrder.CreatedAt,
                OrderDetails = newOrder.OrderDetails.Select(od => new OrderDetailResultDto
                {
                    OrderDetailId = od.OrderDetailId,
                    MenuItemName = menuItemsFromDb.FirstOrDefault(m => m.MenuItemId == od.MenuItemId)?.Name ?? "Không rõ",
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    Status = od.Status
                }).ToList()
            };

            return result; // Trả về DTO (sạch, không có vòng lặp)
        }

        // (Thêm hàm mới này vào OrderTableService.cs)
        //public async Task<bool> CancelOrderItemAsync(int orderDetailId)
        //{
        //    // Lấy món ăn VÀ order cha của nó
        //    var item = await _context.OrderDetails
        //        .Include(od => od.Order)
        //        .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

        //    if (item == null)
        //    {
        //        throw new Exception("Không tìm thấy món ăn.");
        //    }

        //    // === LOGIC "THỜI GIAN ÂN HẬN" ===
        //    if (item.Status == "Đã gửi")
        //    {
        //        item.Status = "Đã hủy";

        //        // Cập nhật lại tổng tiền của Order
        //        if (item.Order != null)
        //        {
        //            item.Order.TotalAmount -= (item.UnitPrice * item.Quantity);
        //        }

        //        await _context.SaveChangesAsync();

        //        // (Sau này thêm SignalR ở đây để báo cho bếp/thu ngân)

        //        return true;
        //    }

        //    // Nếu đã "Đang chế biến" hoặc "Đã lên bàn"
        //    throw new Exception("Món ăn đã được gửi đến bếp, không thể hủy.");
        //}

        // (Trong file BusinessAccessLayer/Services/OrderTableService.cs)

        public async Task<bool> CancelOrderItemAsync(int orderDetailId)
        {
            var item = await _context.OrderDetails
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

            if (item == null)
            {
                throw new Exception("Không tìm thấy món ăn.");
            }

            // === LOGIC KIỂM TRA MÀ BẠN ĐANG TÌM NẰM Ở ĐÂY ===

            // 1. Kiểm tra trạng thái:
            if (item.Status != "Đã gửi")
            {
                throw new Exception("Món ăn đang được chế biến, không thể hủy.");
            }

            // 2. Tính thời gian đã trôi qua
            var timeElapsed = DateTime.UtcNow - item.CreatedAt;

            // 3. Đặt giới hạn 
            const int cancelTimeLimitInMinutes = 2; // SỬA SỐ NÀY TỪ 2 THÀNH ?

            if (timeElapsed.TotalMinutes > cancelTimeLimitInMinutes)
            {
                // Thông báo lỗi nếu quá thời gian
                throw new Exception($"Đã quá {cancelTimeLimitInMinutes} phút, không thể hủy.");
            }

            // Nếu vượt qua kiểm tra -> Tiến hành hủy
            item.Status = "Đã hủy";

            if (item.Order != null)
            {
                item.Order.TotalAmount -= (item.UnitPrice * item.Quantity);
            }

            await _context.SaveChangesAsync();

            return true;
        }


        // Gọi xử lý sự cố
        // 

        public async Task RequestAssistanceAsync(AssistanceRequestDto requestDto)
        {
            // 1. Kiểm tra Reservation
            var reservation = await _orderTableRepository.GetActiveReservationByTableIdAsync(requestDto.TableId);
            if (reservation == null)
            {
                throw new Exception("Bàn không hợp lệ hoặc hiện không có khách.");
            }

            // 2. (CHỐNG SPAM) Kiểm tra 
            bool alreadyPending = await _orderTableRepository.
                HasPendingAssistanceRequestAsync(requestDto.TableId);
            if (alreadyPending)
            {
                throw new Exception("Bạn đã gửi yêu cầu trước đó. Nhân viên sẽ đến ngay!");
            }

            // 3. Tạo yêu cầu mới
            var newRequest = new AssistanceRequest
            {
                TableId = requestDto.TableId,
                ReservationId = reservation.ReservationId,
                RequestTime = DateTime.UtcNow,
                Status = "Pending",
                Note = requestDto.Note,
                HandledTime = null
            };

            await _orderTableRepository.CreateAssistanceRequestAsync(newRequest);
            await _context.SaveChangesAsync();

            // (SignalR logic...)
        }


        // (Trong BusinessAccessLayer/Services/OrderTableService.cs, lồng bên trong)

        //Gọi xử lý sự cố
        public class AssistanceRequestDto
        {
            [Required]
            public int TableId { get; set; }

            [StringLength(500)]
            public string? Note { get; set; }
        }
        // DTO này chứa toàn bộ dữ liệu cho trang Menu
        public class MenuPageViewModel
        {
            public IEnumerable<MenuItemDto> MenuItems { get; set; } = new List<MenuItemDto>();
            public List<OrderDetailStatusDto> OrderedItems { get; set; } = new List<OrderDetailStatusDto>();
            public string TableNumber { get; set; }
            public string AreaName { get; set; }
            public int? Floor { get; set; }
        }

        // DTO này mô tả 1 món đã gọi và trạng thái của nó
        public class OrderDetailStatusDto
        {
            public int OrderDetailId { get; set; }
            public int MenuItemId { get; set; }
            public string ItemName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }

            public string? Notes { get; set; }
        }

        //new DTO for menu and filter
        public class MenuCategoryDto
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; }
        }

        // === THÊM 2 CLASS DTO NÀY VÀO TRONG OrderTableService ===
        public class OrderResultDto
        {
            public int OrderId { get; set; }
            public string? Status { get; set; }
            public decimal? TotalAmount { get; set; }
            public DateTime? CreatedAt { get; set; }
            public List<OrderDetailResultDto> OrderDetails { get; set; } = new();
        }

        public class OrderDetailResultDto
        {
            public int OrderDetailId { get; set; }
            public string MenuItemName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public string Status { get; set; } = string.Empty;
        }

        public class CartItemDto
        {
            public int MenuItemId { get; set; }
            public int Quantity { get; set; }
            public string? Notes { get; set; } // <-- Thêm dòng này
        }
        public class OrderSubmissionDto
        {
            public int TableId { get; set; }
            public List<CartItemDto> Items { get; set; }
        }
        public class TableQRDTO
        {
            [Required]
            public int TableId { get; set; }

            public string TableNumber { get; set; }

            public int Capacity { get; set; }

            public string Status { get; set; }

            // Chúng ta nên thêm cả AreaId và AreaName
            // AreaId để client có thể dùng để lọc
            // AreaName để hiển thị trực tiếp

            public int AreaId { get; set; }

            public string AreaName { get; set; }

            public bool IsAvailable { get; set; }

            public int Floor { get; set; }
        }

        public class MenuItemDto
        {
            public int MenuItemId { get; set; }

            public string Name { get; set; } = string.Empty;

            public string CategoryName { get; set; } = string.Empty;

            public decimal Price { get; set; }

            public string CourseType { get; set; } = string.Empty;

            public bool? IsAvailable { get; set; }

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
            public int? Floor { get; set; }
            public int NumberGuest { get; set; }
        }
        public class PagedTableOrderResult
        {
            public int TotalCount { get; set; }
            public List<TableOrderDto> Items { get; set; } = new(); // Danh sách bàn trang hiện tại
            public int Page { get; set; }
            public int PageSize { get; set; }
            // Tự động tính tổng số trang
            public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        }


    }
}

