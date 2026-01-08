using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Hubs;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Google;
using Microsoft.AspNetCore.SignalR;
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
        private readonly IConfiguration _config;
        private readonly SapaFoRestRmsContext _context;
        private readonly IInventoryIngredientService _inventoryService;
        private readonly IHubContext<RestaurantHub> _hubContext;
        public OrderTableService(
             IOrderTableRepository orderTableRepository,
             IConfiguration config, SapaFoRestRmsContext context,
             IInventoryIngredientService inventoryService,

             IHubContext<RestaurantHub> hubContext)
        {
            _orderTableRepository = orderTableRepository;
            _config = config;
            _context = context;
            _inventoryService = inventoryService;
            _hubContext = hubContext;
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
                    NumberGuest = r.NumberOfGuests
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

        // HÀM GenerateQrCodeForTableAsync 
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
        public async Task<MenuPageViewModel> GetMenuForTableAsync(int tableId, int? categoryId, string? searchString)
        {
            // 1. Lấy chi tiết Bàn (Giữ nguyên)
            var table = await _orderTableRepository.GetByTbIdAsync(tableId);
            if (table == null)
            {
                throw new Exception("Bàn không tồn tại.");
            }

            // 2. Lấy Reservation 
            var reservation = await _context.Reservations
                .Include(r => r.Orders)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.MenuItem)
                .Include(r => r.Orders)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.Combo)
                .Where(r => r.ReservationTables.Any(rt => rt.TableId == tableId)
                && r.Status == "Guest Seated")
                .FirstOrDefaultAsync();

            if (reservation == null)
            {
                throw new Exception("Bàn không hợp lệ hoặc hiện không có khách.");
            }

            // 3. Lấy danh sách món đã gọi
            var orderedItems = reservation.Orders
                .SelectMany(o => o.OrderDetails)
                .Where(od => od.Status != "Đã hủy")
                .Select(od => new OrderDetailStatusDto
                {
                    OrderDetailId = od.OrderDetailId,
                    MenuItemId = od.MenuItemId,
                    ComboId = od.ComboId,

                    // === SỬA LẠI LOGIC LẤY TÊN ===
                    ItemName = od.MenuItemId.HasValue // Kiểm tra xem có phải món ăn không
                                ? od.MenuItem.Name // Nếu có, lấy tên Món
                                : (od.ComboId.HasValue ? od.Combo.Name : "Lỗi dữ liệu"), // Nếu không, lấy tên Combo

                    Quantity = od.Quantity,
                    Status = od.Status,
                    CreatedAt = od.CreatedAt,
                    Notes = od.Notes,
                    Price = od.UnitPrice,
                })
                .OrderByDescending(od => od.CreatedAt)
                .ToList();

            // 4. Khởi tạo danh sách kết quả
            IEnumerable<MenuItemDto> menuDto = new List<MenuItemDto>();
            IEnumerable<ComboOrderDto> comboDto = new List<ComboOrderDto>();

            // 5. PHẦN LOGIC MỚI 
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
                .Include(c => c.ComboItems) 
                    .ThenInclude(ci => ci.MenuItem)
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
        private async Task<IEnumerable<MenuItemDto>> BuildMenuDtoForReservation(
    Reservation reservation,
    int? categoryId,
    string? searchString)
        {
            // 1. Gọi Repository đã được lọc
            var menuItems = await _orderTableRepository.GetAvailableMenuWithCategoryAsync(categoryId, searchString);

            // 2. Code cũ của bạn (Tính toán số lượng đã đặt)
            var orderedItems = reservation.Orders
               .SelectMany(o => o.OrderDetails)
               .Where(od => od.MenuItemId.HasValue) // Lọc bỏ Combo
               .GroupBy(od => od.MenuItemId.Value)
               .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            // 3. Trả về DTO và SẮP XẾP LẠI
            return menuItems.Select(m => new MenuItemDto
            {
                MenuItemId = m.MenuItemId,
                Name = m.Name,
                CategoryName = m.Category?.CategoryName ?? "",
                Price = m.Price,
                ImageUrl = m.ImageUrl,
                IsAvailable = m.IsAvailable,
                IsAds = m.IsAds,
                Quantity = orderedItems.ContainsKey(m.MenuItemId) ? orderedItems[m.MenuItemId] : 0
            })
      
            .OrderByDescending(dto => dto.IsAds.GetValueOrDefault())
            .ThenBy(dto => dto.Name)

            .ToList();
        }



        public async Task<List<string>> GetAreaNamesAsync()
        {
            return await _orderTableRepository.GetDistinctAreaNamesAsync();
        }

        public async Task<List<int?>> GetFloorsAsync()
        {
            return await _orderTableRepository.GetDistinctFloorsAsync();
        }

        public async Task<OrderResultDto> SubmitOrderAsync(SubmitOrderRequest orderDto)
        {

            // 1.1 Kiểm tra Bàn / Reservation
            var reservation = await _orderTableRepository.GetActiveReservationByTableIdAsync(orderDto.TableId);
            if (reservation == null)
            {
                throw new Exception("Không tìm thấy bàn hợp lệ hoặc bàn chưa được kích hoạt.");
            }

            // 1.2 Lấy thông tin MÓN LẺ từ DB (để lấy giá)
            var itemIds = orderDto.Items.Select(i => i.MenuItemId).ToList();
            var menuItemsFromDb = await _orderTableRepository.GetMenuItemsByIdsAsync(itemIds);
            var itemPriceMap = menuItemsFromDb.ToDictionary(m => m.MenuItemId, m => m.Price);

            // 1.3 Lấy thông tin COMBO + CẤU TRÚC COMBO (Quan trọng)
            var comboIds = orderDto.Combos.Select(c => c.ComboId).ToList();

            // Include ComboItems để biết combo gồm những món gì
            var combosFromDb = await _context.Combos.AsNoTracking()
                                             .Include(c => c.ComboItems)
                                             .Where(c => comboIds.Contains(c.ComboId) && c.IsAvailable == true)
                                             .ToListAsync();
            var comboPriceMap = combosFromDb.ToDictionary(c => c.ComboId, c => c.Price);

            // Tạo Order cha
            var newOrder = new Order
            {
                ReservationId = reservation.ReservationId,
                CustomerId = reservation.CustomerId, // Int
                CreatedAt = DateTime.Now,
                OrderType = "Tại bàn",
                Status = "Pending",
                TotalAmount = 0,
                OrderDetails = new List<OrderDetail>() // Khởi tạo list rỗng
            };

            // 2.1 Xử lý MÓN LẺ -> Thêm vào list OrderDetails
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
                        Status = "Pending",
                        CreatedAt = DateTime.Now,
                        Notes = cartItem.Notes ?? "" // Tránh null
                    });
                    newOrder.TotalAmount += (price * cartItem.Quantity);
                }
            }

            foreach (var cartCombo in orderDto.Combos)
            {
                if (comboPriceMap.TryGetValue(cartCombo.ComboId, out var price))
                {
                    // A. Tạo đối tượng OrderDetail cho Combo
                    var comboDetail = new OrderDetail
                    {
                        MenuItemId = null,
                        ComboId = cartCombo.ComboId,
                        Quantity = cartCombo.Quantity,
                        UnitPrice = price,
                        Status = "Pending",
                        CreatedAt = DateTime.Now,
                        Notes = cartCombo.Notes ?? "",

                        OrderComboItems = new List<OrderComboItem>()
                    };

                    // B. Tìm công thức Combo và Add vào list con
                    var comboDefinition = combosFromDb.FirstOrDefault(c => c.ComboId == cartCombo.ComboId);
                    if (comboDefinition != null && comboDefinition.ComboItems != null)
                    {
                        foreach (var component in comboDefinition.ComboItems)
                        {
                            // Thêm trực tiếp vào danh sách con của comboDetail
                            // KHÔNG CẦN gán OrderDetailId thủ công, EF tự lo
                            comboDetail.OrderComboItems.Add(new OrderComboItem
                            {
                                MenuItemId = component.MenuItemId,
                                // Công thức: (Định lượng gốc) * (Số lượng khách gọi)
                                Quantity = component.Quantity * cartCombo.Quantity,
                                Status = "Pending",
                                CreatedAt = DateTime.Now,
                                Notes = cartCombo.Notes ?? "", // Copy ghi chú từ cha
                                IsUrgent = false
                            });
                        }
                    }

                    // C. Thêm cục Combo (đã bao gồm các món con bên trong) vào Order tổng
                    newOrder.OrderDetails.Add(comboDetail);
                    newOrder.TotalAmount += (price * cartCombo.Quantity);
                }
            }

            // Kiểm tra giỏ hàng rỗng
            if (newOrder.OrderDetails.Count == 0)
            {
                throw new Exception("Giỏ hàng trống hoặc các món/combo đã chọn không hợp lệ.");
            }

            try
            {
                _context.Orders.Add(newOrder);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Bắt lỗi chi tiết để dễ debug
                var msg = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Lỗi lưu đơn hàng: {msg}");
            }

            foreach (var orderDetail in newOrder.OrderDetails)
            {
                // TH1: Trừ kho món lẻ
                if (orderDetail.MenuItemId.HasValue && orderDetail.Status == "Pending")
                {

                    var reserveResult = await _inventoryService.ReserveBatchesForOrderDetailAsync(orderDetail.OrderDetailId);


                    if (!reserveResult.success)
                    {
                        Console.WriteLine($"Warning Inventory: {reserveResult.message}");
                    }
                }
            }


            // BƯỚC 5: TRẢ VỀ KẾT QUẢ (DTO)
            var result = new OrderResultDto
            {
                OrderId = newOrder.OrderId,
                Status = newOrder.Status,
                TotalAmount = newOrder.TotalAmount,
                CreatedAt = newOrder.CreatedAt,
                // Mapping dữ liệu để trả về Frontend
                OrderDetails = newOrder.OrderDetails.Select(od =>
                {
                    string itemName = "Không rõ";
                    string itemType = "Khác";

                    if (od.MenuItemId.HasValue)
                    {
                        itemName = menuItemsFromDb.FirstOrDefault(m => m.MenuItemId == od.MenuItemId)?.Name ?? "Món lẻ";
                        itemType = "Món ăn";
                    }
                    else if (od.ComboId.HasValue)
                    {
                        itemName = combosFromDb.FirstOrDefault(c => c.ComboId == od.ComboId)?.Name ?? "Combo";
                        itemType = "Combo";
                    }

                    return new OrderDetailResultDto
                    {
                        OrderDetailId = od.OrderDetailId,
                        ItemName = itemName,
                        ItemType = itemType,
                        Quantity = od.Quantity,
                        UnitPrice = od.UnitPrice,
                        Status = od.Status
                    };
                }).ToList()
            };

            return result;
        }

        public async Task<bool> CancelOrderItemAsync(int orderDetailId)
        {
            // 1. Lấy thông tin món + Kèm theo OrderComboItems (WITH TRACKING để có thể update)
            var item = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.OrderComboItems)
                .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

            if (item == null)
            {
                throw new Exception("Không tìm thấy món ăn.");
            }

            // 2. KIỂM TRA TRẠNG THÁI MÓN CHA - cho phép hủy khi Pending hoặc Cooking
            if (item.Status != "Pending" && item.Status != "Cooking")
            {
                throw new Exception("Chỉ có thể hủy món đang chờ hoặc đang nấu. Vui lòng kiểm tra trạng thái món.");
            }


            if (item.OrderComboItems != null && item.OrderComboItems.Any())
            {
                // FIX: Kiểm tra xem có món nào trong combo đã nấu/sẵn sàng/hoàn thành không
                // Bao gồm: Cooking, Ready, Done, Served
                bool hasCookedItem = item.OrderComboItems.Any(c => 
                {
                    var status = (c.Status ?? "Pending").Trim();
                    var statusLower = status.ToLower();
                    return statusLower.Contains("cooking") || 
                           statusLower.Contains("đang nấu") ||
                           statusLower.Contains("ready") || 
                           statusLower.Contains("sẵn sàng") ||
                           statusLower.Contains("done") || 
                           statusLower.Contains("hoàn thành") ||
                           statusLower.Contains("xong") ||
                           statusLower.Contains("served");
                });

                if (hasCookedItem)
                {
                    throw new Exception("Đã có món trong combo được nấu/sẵn sàng/hoàn thành nên sẽ không hủy được. Vui lòng chờ !");
                }
            }

            //  Giải phóng reserved quantity TRƯỚC khi cập nhật status
            // Nếu món đã được reserve nguyên liệu, cần giải phóng để available có thể tăng lại
            var releaseResult = await _inventoryService.ReleaseReservedBatchesForOrderDetailAsync(orderDetailId);
            if (!releaseResult.success)
            {
                Console.WriteLine($"Warning: Không thể giải phóng nguyên liệu khi hủy món: {releaseResult.message}");
            }

            // 3. Cập nhật trạng thái món Cha
            item.Status = "Cancelled";

            // 4. Cập nhật trạng thái các món Con
            if (item.OrderComboItems != null && item.OrderComboItems.Any())
            {
                foreach (var childItem in item.OrderComboItems)
                {
                    // Chỉ hủy các món đang chờ
                    if (childItem.Status == "Pending")
                    {
                        childItem.Status = "Cancelled";
                    }
                }
            }

            // 5. Cập nhật lại tổng tiền hóa đơn
            if (item.Order != null)
            {
                item.Order.TotalAmount -= (item.UnitPrice * item.Quantity);

                if (item.Order.TotalAmount < 0) item.Order.TotalAmount = 0;
            }

            // 6. Lưu thay đổi
            await _context.SaveChangesAsync();

            return true;
        }


        public async Task RequestAssistanceAsync(AssistanceRequestDto requestDto)
        {
            // BƯỚC 1: KIỂM TRA SPAM (Dùng AsNoTracking để không giữ lock DB)
            bool alreadyPending = await _context.AssistanceRequests
                .AsNoTracking() // <--- QUAN TRỌNG: Chỉ đọc, không theo dõi
                .AnyAsync(r => r.TableId == requestDto.TableId && r.Status == "Pending");

            if (alreadyPending)
            {
                // Trả về lỗi rõ ràng để Frontend biết mà hiển thị
                throw new Exception("Yêu cầu trước đó đang chờ xử lý. Vui lòng đợi nhân viên!");
            }

            // BƯỚC 2: KIỂM TRA RESERVATION
            int? reservationId = null;
            if (requestDto.UseReservation)
            {
                // Tự query trực tiếp ở đây dùng AsNoTracking để nhẹ nhất có thể
                var reservation = await _context.ReservationTables
                    .AsNoTracking()
                    .Include(rt => rt.Reservation)
                    .Where(rt => rt.TableId == requestDto.TableId
                                 && (rt.Reservation.Status == "Active" || rt.Reservation.Status == "Guest Seated"))
                    .Select(rt => rt.Reservation)
                    .FirstOrDefaultAsync();

                if (reservation == null)
                {
                    throw new Exception("Bàn không hợp lệ hoặc hiện không có khách check-in.");
                }
                reservationId = reservation.ReservationId;
            }

            // BƯỚC 3: TẠO REQUEST MỚI
            var note = string.IsNullOrWhiteSpace(requestDto.Note) ? "Khách cần hỗ trợ !" : requestDto.Note;
            var newRequest = new AssistanceRequest
            {
                TableId = requestDto.TableId,
                ReservationId = reservationId,
                RequestTime = DateTime.Now,
                Status = "Pending",
                Note = note,
                HandledTime = null
            };

            _context.AssistanceRequests.Add(newRequest);
            await _context.SaveChangesAsync();

            // BƯỚC 4: LẤY THÔNG TIN BÀN ĐỂ GỬI SOCKET (Dùng AsNoTracking)
            var tableInfo = await _context.Tables
                .AsNoTracking() // <--- QUAN TRỌNG
                .Include(t => t.Area)
                .Where(t => t.TableId == requestDto.TableId)
                .Select(t => new
                {
                    t.TableNumber,
                    AreaName = t.Area.AreaName
                })
                .FirstOrDefaultAsync();

            var tableName = tableInfo?.TableNumber ?? requestDto.TableId.ToString();
            var areaName = tableInfo?.AreaName ?? "Không xác định";

            // BƯỚC 5: GỬI SOCKET (Bọc try-catch để không chết app nếu lỗi mạng)
            try
            {
                await _hubContext.Clients.Group("Employees").SendAsync("ReceiveNewRequest", new
                {
                    requestId = newRequest.RequestId,
                    tableId = newRequest.TableId,
                    tableName = $"Bàn {tableName}",
                    areaName = areaName,
                    note = newRequest.Note,
                    time = newRequest.RequestTime.ToLocalTime().ToString("HH:mm")
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi socket: {ex.Message}");
                // Không throw exception ở đây để đảm bảo request vẫn được lưu vào DB thành công
            }
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



        // 1. HÀM LẤY DANH SÁCH (Cho màn hình nhân viên)
        public async Task<DTOs.OrderAssitance.PagedResult<AssistanceResponseDto>> GetStaffPendingRequestsAsync(
     string? sort, int page, int pageSize)
        {
            // Bỏ areaId, dùng sort (newest/oldest)
            var (items, totalCount) = await _orderTableRepository.GetPendingRequestsForStaffAsync(
                sort, page, pageSize);

            var dtos = items.Select(x => new AssistanceResponseDto
            {
                RequestId = x.RequestId,
                TableName = x.Table != null ? $"Bàn {x.Table.TableNumber}" : "Bàn ?",
                AreaName = x.Table?.Area?.AreaName ?? "N/A",
                Floor = x.Table?.Area?.Floor ?? 0,
                Note = x.Note,
                RequestTime = x.RequestTime,
                TimeAgo = CalculateTimeAgo(x.RequestTime) // Hàm phụ tính thời gian
            }).ToList();

            return new BusinessAccessLayer.DTOs.OrderAssitance.PagedResult<AssistanceResponseDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }


        // 2. HÀM XỬ LÝ YÊU CẦU (Nhân viên ấn "Xong")
        public async Task CompleteAssistanceRequestAsync(int requestId)
        {
            // a. Lấy yêu cầu từ DB
            var request = await _orderTableRepository.GetRequestByIdAsync(requestId);
            if (request == null) throw new Exception("Yêu cầu không tồn tại hoặc đã bị xóa.");

            // b. Cập nhật trạng thái
            request.Status = "Completed";
            request.HandledTime = DateTime.Now; // Lưu thời gian xử lý

            // c. Lưu DB
            await _context.SaveChangesAsync();

            // d. SIGNALR: Bắn sự kiện "Đã xong"
            // -> Để xóa dòng đó khỏi màn hình của TẤT CẢ nhân viên khác ngay lập tức
            await _hubContext.Clients.Group("Employees").SendAsync("RequestCompleted", requestId);

            // (Tùy chọn) Báo cho khách biết nhân viên đang tới
            // await _hubContext.Clients.Group($"Table-{request.TableId}").SendAsync("StaffArrived", "Nhân viên đã tiếp nhận!");
        }

        // Hàm phụ tính thời gian
        private string CalculateTimeAgo(DateTime created)
        {
            var diff = DateTime.UtcNow - created;
            if (diff.TotalMinutes < 1) return "Vừa xong";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
            return $"{(int)diff.TotalHours} giờ trước";
        }
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

            public bool UseReservation { get; set; } = true; // mặc định là true

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

            public decimal Price { get; set; }

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

            public bool? IsAds { get; set; }

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

