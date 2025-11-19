using BusinessAccessLayer.DTOs;
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
            // 1. Lấy chi tiết Bàn (Giữ nguyên)
            var table = await _orderTableRepository.GetByTbIdAsync(tableId);
            if (table == null)
            {
                throw new Exception("Bàn không tồn tại.");
            }

            // 2. Lấy Reservation (ĐÃ SỬA: Phải Include cả MenuItem VÀ Combo)
            var reservation = await _context.Reservations
                .Include(r => r.Orders)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.MenuItem) // <-- Cần cho tên món
                .Include(r => r.Orders) // <-- THÊM MỚI
                    .ThenInclude(o => o.OrderDetails) // <-- THÊM MỚI
                        .ThenInclude(od => od.Combo) // <-- THÊM MỚI (Rất quan trọng)
                .Where(r => r.ReservationTables.Any(rt => rt.TableId == tableId)
                && r.Status == "Guest Seated")
                .FirstOrDefaultAsync();

            if (reservation == null)
            {
                throw new Exception("Bàn không hợp lệ hoặc hiện không có khách.");
            }

            // 3. Lấy danh sách món đã gọi (ĐÃ SỬA: Xử lý cả Món và Combo)
            var orderedItems = reservation.Orders
                .SelectMany(o => o.OrderDetails)
                .Where(od => od.Status != "Đã hủy")
                .Select(od => new OrderDetailStatusDto
                {
                    OrderDetailId = od.OrderDetailId,
                    MenuItemId = od.MenuItemId, // Gán int? (an toàn)
                    ComboId = od.ComboId,       // Gán int? (an toàn)

                    // === SỬA LẠI LOGIC LẤY TÊN ===
                    ItemName = od.MenuItemId.HasValue // Kiểm tra xem có phải món ăn không
                                ? od.MenuItem.Name // Nếu có, lấy tên Món
                                : (od.ComboId.HasValue ? od.Combo.Name : "Lỗi dữ liệu"), // Nếu không, lấy tên Combo

                    Quantity = od.Quantity,
                    Status = od.Status,
                    CreatedAt = od.CreatedAt,
                    Notes = od.Notes
                })
                .OrderByDescending(od => od.CreatedAt)
                .ToList();

            // 4. Khởi tạo danh sách kết quả (Giữ nguyên)
            IEnumerable<MenuItemDto> menuDto = new List<MenuItemDto>();
            IEnumerable<ComboOrderDto> comboDto = new List<ComboOrderDto>();

            // 5. PHẦN LOGIC MỚI (Giữ nguyên)
            if (categoryId.HasValue && categoryId == -1) // Người dùng bấm tab "Combos"
            {
                comboDto = await BuildComboDtoAsync(searchString);
            }
            else if (!categoryId.HasValue || categoryId == 0) // Người dùng bấm tab "Tất cả"
            {
                menuDto = await BuildMenuDtoForReservation(reservation, null, searchString);
                comboDto = await BuildComboDtoAsync(searchString);
            }
            else // Người dùng bấm category cụ thể
            {
                menuDto = await BuildMenuDtoForReservation(reservation, categoryId, searchString);
            }

            // 6. Trả về ViewModel (Giữ nguyên)
            return new MenuPageViewModel
            {
                MenuItems = menuDto,
                Combos = comboDto,
                OrderedItems = orderedItems,
                TableNumber = table.TableNumber,
                AreaName = table.Area?.AreaName ?? "N/A",
                Floor = table.Area?.Floor
            };
        }

        // Lấy category và combo
        public async Task<IEnumerable<MenuCategoryDto>> GetMenuCategoriesAsync()
        {
            var categories = await _orderTableRepository.GetAllCategoriesAsync();

            // 1. Chuyển đổi sang List DTO
            var categoryDtos = categories.Select(c => new MenuCategoryDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName
            }).ToList(); // <-- Chuyển sang ToList()

            // 2. THÊM DANH MỤC ẢO
            // Thêm "Tất cả" (nếu bạn muốn)
            //categoryDtos.Insert(0, new MenuCategoryDto
            //{
            //    CategoryId = 0, // Hoặc null, tùy cách bạn xử lý "Tất cả"
            //    CategoryName = "Tất cả"
            //});

            // Thêm "Combos"
            categoryDtos.Add(new MenuCategoryDto
            {
                CategoryId = -1, // <-- ID ảo đặc biệt
                CategoryName = "Combos"
            });

            return categoryDtos;
        }
        // (Hàm private helper trong OrderTableService.cs)
        // Đặt hàm này bên trong class OrderTableService
        // Trong file OrderTableService.cs
        public async Task<IEnumerable<ComboOrderDto>> BuildComboDtoAsync(string? searchString)
        {
            var query = _context.Combos
                .Include(c => c.ComboItems) // <-- BẮT BUỘC INCLUDE
                    .ThenInclude(ci => ci.MenuItem) // <-- BẮT BUỘC INCLUDE
                .Where(c => c.IsAvailable == true);

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.Name.ToLower().Contains(searchString.ToLower()));
            }

            var combos = await query.ToListAsync();

            // Dùng Select (của LINQ to Objects) để tính toán
            return combos.Select(c => new ComboOrderDto
            {
                ComboId = c.ComboId,
                Name = c.Name,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                Price = c.Price, // Giá đã giảm (369k)

                // === TÍNH TOÁN MỚI ===
                // Lấy tổng giá gốc của các món lẻ bên trong
                OriginalPrice = c.ComboItems.Sum(ci =>
                    (ci.MenuItem != null ? ci.MenuItem.Price * ci.Quantity : 0)
                )
            });
        }

        // === SỬA HÀM NÀY ===
        private async Task<IEnumerable<MenuItemDto>> BuildMenuDtoForReservation(
      Reservation reservation,
      int? categoryId,
      string? searchString)
        {
            // 1. Gọi Repository đã được lọc
            var menuItems = await _orderTableRepository.GetAvailableMenuWithCategoryAsync(categoryId, searchString);

            // 2. Code cũ của bạn
            var orderedItems = reservation.Orders
             .SelectMany(o => o.OrderDetails)

             // === THÊM DÒNG NÀY ĐỂ LỌC BỎ COMBO ===
             .Where(od => od.MenuItemId.HasValue) // Chỉ lấy các chi tiết là MÓN ĂN
                                                  // ===================================

             .GroupBy(od => od.MenuItemId.Value) // Giờ có thể dùng .Value an toàn
             .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            // 3. Trả về DTO (Giữ nguyên)
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
        // Sửa tên DTO đầu vào thành DTO mới: SubmitOrderRequest
        public async Task<OrderResultDto> SubmitOrderAsync(SubmitOrderRequest orderDto)
        {
            // 1. Kiểm tra Reservation (Giữ nguyên)
            var reservation = await _orderTableRepository.GetActiveReservationByTableIdAsync(orderDto.TableId);
            if (reservation == null)
            {
                throw new Exception("Không tìm thấy bàn hợp lệ hoặc bàn chưa được kích hoạt.");
            }

            // 2. Lấy giá gốc MÓN LẺ từ DB (Giữ nguyên)
            var itemIds = orderDto.Items.Select(i => i.MenuItemId).ToList();
            var menuItemsFromDb = await _orderTableRepository.GetMenuItemsByIdsAsync(itemIds);
            var itemPriceMap = menuItemsFromDb.ToDictionary(m => m.MenuItemId, m => m.Price);

            // 3. Lấy giá gốc COMBO từ DB (Giữ nguyên)
            var comboIds = orderDto.Combos.Select(c => c.ComboId).ToList();
            var combosFromDb = await _context.Combos.AsNoTracking() // <-- THÊM DÒNG NÀY
                                             .Where(c => comboIds.Contains(c.ComboId) && c.IsAvailable == true)
                                             .ToListAsync();
            var comboPriceMap = combosFromDb.ToDictionary(c => c.ComboId, c => c.Price);

            // 4. Tạo Order (ĐÃ SỬA: Trả lại code CustomerId như cũ)
            var newOrder = new Order
            {
                ReservationId = reservation.ReservationId,

                // === TRẢ LẠI CODE CŨ CỦA BẠN ===
                // Vì CustomerId là 'int', chúng ta gán thẳng
                CustomerId = reservation.CustomerId,

                CreatedAt = DateTime.UtcNow,
                OrderType = "Tại bàn",
                Status = "Pending",
                TotalAmount = 0,
                OrderDetails = new List<OrderDetail>()
            };

            // 5. Tạo OrderDetails cho MÓN LẺ (Giữ nguyên)
            foreach (var cartItem in orderDto.Items)
            {
                if (itemPriceMap.TryGetValue(cartItem.MenuItemId, out var price))
                {
                    newOrder.OrderDetails.Add(new OrderDetail
                    {
                        MenuItemId = cartItem.MenuItemId,
                        ComboId = null,
                        Quantity = cartItem.Quantity,
                        UnitPrice = price,
                        Status = "Đã gửi",
                        CreatedAt = DateTime.UtcNow,
                        Notes = cartItem.Notes,
                    });
                    newOrder.TotalAmount += (price * cartItem.Quantity);
                }
            }

            // 6. Tạo OrderDetails cho COMBO (Giữ nguyên)
            foreach (var cartCombo in orderDto.Combos)
            {
                if (comboPriceMap.TryGetValue(cartCombo.ComboId, out var price))
                {
                    newOrder.OrderDetails.Add(new OrderDetail
                    {
                        MenuItemId = null,
                        ComboId = cartCombo.ComboId,
                        Quantity = cartCombo.Quantity,
                        UnitPrice = price,
                        Status = "Đã gửi",
                        CreatedAt = DateTime.UtcNow,
                        Notes = cartCombo.Notes,
                    });
                    newOrder.TotalAmount += (price * cartCombo.Quantity);
                }
            }

            // 7. Kiểm tra giỏ hàng rỗng (Giữ nguyên)
            if (newOrder.OrderDetails.Count == 0)
            {
                throw new Exception("Giỏ hàng trống hoặc các món/combo đã chọn không hợp lệ.");
            }

            // 8. Lưu vào Database (Giữ nguyên)
            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            // 9. Trả về DTO kết quả (Giữ nguyên bản sửa lỗi an toàn)
            var result = new OrderResultDto
            {
                OrderId = newOrder.OrderId,
                Status = newOrder.Status,
                TotalAmount = newOrder.TotalAmount,
                CreatedAt = newOrder.CreatedAt,
                OrderDetails = newOrder.OrderDetails.Select(od =>
                {
                    if (od.MenuItemId.HasValue) // Đây là món lẻ
                    {
                        return new OrderDetailResultDto
                        {
                            OrderDetailId = od.OrderDetailId,
                            ItemName = menuItemsFromDb.FirstOrDefault(m => m.MenuItemId == od.MenuItemId.Value)?.Name ?? "Món không rõ",
                            ItemType = "Món ăn",
                            Quantity = od.Quantity,
                            UnitPrice = od.UnitPrice,
                            Status = od.Status
                        };
                    }
                    else if (od.ComboId.HasValue) // <-- Đây là bản sửa lỗi ĐÚNG
                    {
                        return new OrderDetailResultDto
                        {
                            OrderDetailId = od.OrderDetailId,
                            ItemName = combosFromDb.FirstOrDefault(c => c.ComboId == od.ComboId.Value)?.Name ?? "Combo không rõ",
                            ItemType = "Combo",
                            Quantity = od.Quantity,
                            UnitPrice = od.UnitPrice,
                            Status = od.Status
                        };
                    }
                    else // Trường hợp hi hữu cả 2 đều null
                    {
                        return new OrderDetailResultDto
                        {
                            OrderDetailId = od.OrderDetailId,
                            ItemName = "Lỗi dữ liệu",
                            ItemType = "Lỗi",
                            Quantity = od.Quantity,
                            UnitPrice = od.UnitPrice,
                            Status = od.Status
                        };
                    }
                }).ToList()
            };

            return result; // Trả về DTO
        }


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

        public async Task<ComboDetailDto> GetComboDetailsAsync(int comboId)
        {
            // 1. Gọi Repository
            var combo = await _orderTableRepository.GetComboWithDetailsAsync(comboId);

            if (combo == null)
            {
                throw new Exception("Không tìm thấy combo hợp lệ hoặc combo đã bị ẩn.");
            }

            // 2. Chuyển đổi (Map) các món con
            var itemDtos = combo.ComboItems.Select(ci => new ComboItemDetailDto
            {
                // Dùng `?` (null-conditional) để phòng trường hợp MenuItem đã bị xóa
                ItemName = ci.MenuItem?.Name ?? "Món không xác định",
                Quantity = ci.Quantity,
                UnitPrice = ci.MenuItem?.Price ?? 0,
                ImageUrl = ci.MenuItem.ImageUrl
            }).ToList();

            // 3. Tính toán giá
            // (Đây là logic bạn đã làm ở Yêu cầu 3)
            decimal originalPrice = itemDtos.Sum(item => item.UnitPrice * item.Quantity);
            decimal savedAmount = 0;

            // Chỉ tính "tiết kiệm" nếu giá gốc > 0 (tránh chia cho 0)
            // và giá gốc > giá combo
            if (originalPrice > 0 && originalPrice > combo.Price)
            {
                savedAmount = originalPrice - combo.Price;
            }

            // 4. Tạo DTO trả về
            return new ComboDetailDto
            {
                ComboId = combo.ComboId,
                Name = combo.Name,
                ImageUrl = combo.ImageUrl,
                ComboPrice = combo.Price,        // Giá đã giảm (369k)
                OriginalPrice = originalPrice, // Giá gốc (419k)
                SavedAmount = savedAmount,     // Tiết kiệm (50k)
                Items = itemDtos
            };
        }

        public async Task<MenuItemDetailDto> GetMenuItemDetailsAsync(int menuItemId)
        {
            // 1. Gọi Repository
            var menuItem = await _orderTableRepository.GetMenuItemWithDetailsAsync(menuItemId);

            if (menuItem == null || menuItem.IsAvailable == false)
            {
                throw new Exception("Không tìm thấy món ăn hợp lệ.");
            }

            // 2. Map sang DTO
            return new MenuItemDetailDto
            {
                MenuItemId = menuItem.MenuItemId,
                Name = menuItem.Name,
                ImageUrl = menuItem.ImageUrl,
                Price = menuItem.Price,
                Description = menuItem.Description,
                CategoryName = menuItem.Category?.CategoryName ?? "Không xác định"
            };
        }
        // (Trong BusinessAccessLayer/Services/OrderTableService.cs, lồng bên trong)

        // 3. DTO tổng hợp (Đây là thứ mà API sẽ nhận)

        // DTO cho chi tiết món ăn 
        public class MenuItemDetailDto
        {
            public int MenuItemId { get; set; }
            public string Name { get; set; }
            public string ImageUrl { get; set; }
            public decimal Price { get; set; }
            public string Description { get; set; }
            public string CategoryName { get; set; }
        }

        // DTO cho các món ăn bên trong combo
        public class ComboItemDetailDto
        {
            public string ItemName { get; set; }
            public int Quantity { get; set; } // Số lượng món (vd: 2 Gà, 1 Pepsi)
            public decimal UnitPrice { get; set; } // Giá của 1 món lẻ

            public string ImageUrl { get; set; }
        }

        // DTO cho toàn bộ cửa sổ (Modal) chi tiết
        public class ComboDetailDto
        {
            public int ComboId { get; set; }
            public string Name { get; set; }
            public string ImageUrl { get; set; }
            public decimal ComboPrice { get; set; }    // Giá đã giảm (369k)
            public decimal OriginalPrice { get; set; } // Giá gốc (419k)
            public decimal SavedAmount { get; set; }   // Tiết kiệm (50k)
            public List<ComboItemDetailDto> Items { get; set; }
        }
        public class OrderItemDto
        {
            public int MenuItemId { get; set; }
            public int Quantity { get; set; }
            public string Notes { get; set; }
        }

        public class SubmitOrderRequest
        {
            public int TableId { get; set; }
            public List<OrderItemDto> Items { get; set; }
            public List<OrderComboDto> Combos { get; set; }
        }

        public class OrderComboDto
        {
            public int ComboId { get; set; }
            public int Quantity { get; set; }
            public string Notes { get; set; }
        }
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
            public IEnumerable<ComboOrderDto> Combos { get; set; } // <-- THÊM DÒNG NÀY
            public string TableNumber { get; set; }
            public string AreaName { get; set; }
            public int? Floor { get; set; }
        }

        // DTO này mô tả 1 món đã gọi và trạng thái của nó
        public class OrderDetailStatusDto
        {
            public int OrderDetailId { get; set; }
            public int? MenuItemId { get; set; }
            public int? ComboId { get; set; }
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
            public string ItemName { get; set; } // Tên chung cho cả món/combo
            public string ItemType { get; set; } // "Món ăn" hoặc "Combo"
            public string MenuItemName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public string Status { get; set; } = string.Empty;
        }

        public class CartItemDto
        {
            public int MenuItemId { get; set; }
            public int Quantity { get; set; }
            public string? Notes { get; set; } 
        }
        public class OrderSubmissionDto
        {
            public int TableId { get; set; }
            public List<CartItemDto> Items { get; set; }
            public List<ComboOrderDto> Combos { get; set; } // Danh sách combo
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

