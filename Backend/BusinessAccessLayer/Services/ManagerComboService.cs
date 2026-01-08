using AutoMapper;
using BusinessAccessLayer.DTOs.ManagementCombo;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using static BusinessAccessLayer.DTOs.ManagementCombo.UpdateDtosCombo;
using static BusinessAccessLayer.Services.OrderTableService;
using ComboDetailDto = BusinessAccessLayer.DTOs.ManagementCombo.UpdateDtosCombo.ComboDetailDto;
using ComboItemDto = BusinessAccessLayer.DTOs.ManagementCombo.UpdateDtosCombo.ComboItemDto;
using MenuItemDto = BusinessAccessLayer.DTOs.ManagementCombo.UpdateDtosCombo.MenuItemDto;

namespace BusinessAccessLayer.Services
{
    public class ManagerComboService : IManagerComboService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IManagerComboRepository _repo;

        public ManagerComboService(IUnitOfWork unitOfWork, IMapper mapper, IManagerComboRepository repository)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _repo = repository;
        }
        public async Task<IEnumerable<ManagerComboDTO>> GetManagerAllCombo()
        {
            var combo = await _unitOfWork.Combo.GetManagerAllCombos();
            return _mapper.Map<IEnumerable<ManagerComboDTO>>(combo);
        }
        // 1. Lấy danh sách Menu (Có phân trang)
        public async Task<DTOs.ManagementCombo.PagedResult<DTOs.ManagementCombo.MenuItemDto>> GetMenuItemsAsync(MenuFilterRequest request)
        {
            // Gọi Repo lấy Query
            var query = _repo.GetMenuItemsQuery(request.Keyword, request.MinPrice, request.MaxPrice, request.SortBy, request.IsAvaiable);

            // Thực hiện phân trang tại Service (hoặc dùng Extension Method)
            int totalRow = await query.CountAsync();
            var entities = await query.Skip((request.PageIndex - 1) * request.PageSize)
                                      .Take(request.PageSize)
                                      .ToListAsync();

            // Map Entity -> DTO
            var dtos = entities.Select(e => new DTOs.ManagementCombo.MenuItemDto
            {
                MenuItemId = e.MenuItemId,
                Name = e.Name,
                Price = e.Price,
                ImageUrl = e.ImageUrl,
                CategoryName = e.Category.CategoryName,
                IsAvailable = e.OrderDetails.Any(od => od.Combo.IsAvailable == true)
            }).ToList();

            return new DTOs.ManagementCombo.PagedResult<DTOs.ManagementCombo.MenuItemDto>(dtos, totalRow, request.PageIndex, request.PageSize);
        }

        // 2. Tạo Combo (Logic tính toán tiền nằm ở đây)
        public async Task CreateComboAsync(CreateComboRequest request)
        {
            decimal calculatedTempPrice = 0;
            var comboItemsList = new List<ComboItem>();

            // Logic: Tính tổng tiền tạm tính
            foreach (var itemDto in request.Items)
            {
                var menuItem = await _repo.GetMenuItemByIdAsync(itemDto.MenuItemId);
                if (menuItem != null)
                {
                    calculatedTempPrice += (menuItem.Price * itemDto.Quantity);
                    comboItemsList.Add(new ComboItem
                    {
                        MenuItemId = menuItem.MenuItemId,
                        Quantity = itemDto.Quantity
                    });
                }
            }

            // Map request -> Entity Combo
            var newCombo = new Combo
            {
                Name = request.Name,
                Price = request.ActualPrice, // Giá nhập tay
                Description = $"Tổng giá trị thực: {calculatedTempPrice}. Giá bán: {request.ActualPrice}", // Lưu note nếu cần
                IsAvailable = true
            };

            // Gọi Repo lưu
            int comboId = await _repo.CreateComboAsync(newCombo);

            // Gán ComboId cho các items và lưu
            comboItemsList.ForEach(x => x.ComboId = comboId);
            await _repo.AddComboItemsAsync(comboItemsList);
        }

        // 3 & 4. Lấy Top Seller (Map KeyValuePair -> DTO)
        public async Task<List<TopSellerDto>> GetTopSellersAsync(string type)
        {
            if (type == "menu")
            {
                var data = await _repo.GetTopSellingMenuItemsAsync(5);
                return data.Select(x => new TopSellerDto
                {
                    Name = x.Key.Name,
                    ImageUrl = x.Key.ImageUrl,
                    TotalSold = x.Value
                }).ToList();
            }
            else
            {
                var data = await _repo.GetTopSellingCombosAsync(5);
                return data.Select(x => new TopSellerDto
                {
                    Name = x.Key.Name,
                    ImageUrl = x.Key.ImageUrl,
                    TotalSold = x.Value
                }).ToList();
            }
        }

        // 5. Thống kê theo Tuần/Tháng/Năm
        public async Task<List<StatsDto>> GetComboSalesStatsAsync(string type)
        {
            DateTime fromDate = DateTime.Now;
            DateTime toDate = DateTime.Now;

            // Xác định khoảng thời gian cần lấy dữ liệu từ DB
            if (type == "week") fromDate = DateTime.Now.AddDays(-7);
            if (type == "month") fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (type == "year") fromDate = new DateTime(DateTime.Now.Year, 1, 1);

            // Lấy dữ liệu thô từ Repo (List<Tuple<Date, Int>>)
            var rawData = await _repo.GetComboSalesByDateRangeAsync(fromDate, toDate);

            // Xử lý Grouping logic tại Service (Code C# thuần)
            // Ví dụ: Group theo Tháng cho báo cáo Năm
            if (type == "year")
            {
                return rawData.GroupBy(x => x.Item1.Month)
                              .Select(g => new StatsDto
                              {
                                  Label = $"Tháng {g.Key}",
                                  Value = g.Sum(x => x.Item2)
                              }).ToList();
            }

            // Mặc định trả về theo ngày
            return rawData.Select(x => new StatsDto
            {
                Label = x.Item1.ToString("dd/MM"),
                Value = x.Item2
            }).ToList();
        }


        public PagedResult<ComboDisplayDto> GetComboDisplayList(string? search,
        bool? isAvailable,
        int pageIndex,
        int pageSize)
        {
            var query = _repo.GetComboQuery(search, isAvailable);

            int totalRecords = query.Count();

            var combos = query
                .OrderBy(c => c.ComboId)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Map sang DTO
            var items = combos.Select(c => new ComboDisplayDto
            {
                ComboId = c.ComboId,
                ComboName = c.Name,
                Description = c.Description,
                Price = c.Price,
                ImageUrl = c.ImageUrl,

                MenuItems = c.ComboItems
                    .Select(ci => ci.MenuItem.Name)
                    .ToList(),

                WeeklyUsed = c.OrderDetails
                    .Count(od => od.CreatedAt >= DateTime.Now.AddDays(-7)),

                MonthlyUsed = c.OrderDetails
                    .Count(od => od.CreatedAt >= DateTime.Now.AddMonths(-1))
            }).ToList();

            // Trả về đúng dạng PagedResult<T>
            return new PagedResult<ComboDisplayDto>(
                items,
                totalRecords,
                pageIndex,
                pageSize
            );
        }

        public async Task<DTOs.ManagementCombo.ComboDetailDto> GetComboByIdAsync(int id)
        {
            // 1. Lấy dữ liệu Entity từ Repo
            var comboEntity = await _repo.GetComboByIdWithItemsAsync(id);

            if (comboEntity == null) throw new KeyNotFoundException("Combo not found");

            // 2. Chuẩn bị list MenuItemDto
            var itemDtos = new List<DTOs.ManagementCombo.MenuItemDto>();
            decimal calculatedOriginalPrice = 0;

            // 3. Duyệt qua từng món trong Combo để: Map dữ liệu + Cộng tiền
            foreach (var comboItem in comboEntity.ComboItems)
            {
                // Tính tổng tiền gốc: Giá món lẻ * Số lượng trong combo
                // Ví dụ: 2 Pepsi (10k) = 20k
                calculatedOriginalPrice += (comboItem.MenuItem.Price * comboItem.Quantity);

                // Add vào list
                itemDtos.Add(new DTOs.ManagementCombo.MenuItemDto
                {
                    MenuItemId = comboItem.MenuItemId,
                    Name = comboItem.MenuItem.Name,
                    Price = comboItem.MenuItem.Price, // Giá gốc 1 món
                    ImageUrl = comboItem.MenuItem.ImageUrl,
                    Quantity = comboItem.Quantity,     // Số lượng (Lấy từ bảng trung gian)
                    CategoryName = comboItem.MenuItem.Category.CategoryName
                });
            }

            // 4. Trả về DTO đúng khuôn mẫu bạn yêu cầu
            return new DTOs.ManagementCombo.ComboDetailDto
            {
                ComboId = comboEntity.ComboId,
                Name = comboEntity.Name,
                ImageUrl = comboEntity.ImageUrl,
                // Mapping giá
                SellingPrice = comboEntity.Price,
                OriginalPrice = calculatedOriginalPrice,

                // SavingsAmount tự động tính trong class DTO (Original - Selling)

                Items = itemDtos
            };
        }


        // Thêm combo
        public async Task AddComboAsync(CreateComboDto request)
        {
            if (request.Items == null || !request.Items.Any())
                throw new ArgumentException("Combo must have at least one item.");

            if (request.Items.Count == 1 && request.Items[0].Quantity < 2)
            {
                throw new ArgumentException("Combo chỉ có 1 món ăn thì quantity phải ≥ 2.");
            }

            // Nếu combo >= 2 món, chỉ cần quantity ≥ 1
            foreach (var item in request.Items)
            {
                if (item.Quantity < 1)
                    throw new ArgumentException($"Item với MenuItemId {item.MenuItemId} phải có quantity ≥ 1.");
            }


            var combo = new Combo
            {
                Name = request.Name,
                Price = request.SellingPrice,
                Description = request.Description,
                IsAvailable = request.IsAvailable,
                ImageUrl = request.ImageUrl
            };

            var items = request.Items.Select(x => new ComboItem
            {
                MenuItemId = x.MenuItemId,
                Quantity = x.Quantity
            }).ToList();

            await _repo.AddComboAsync(combo, items);
        }


        public async Task<ComboDetailDto> GetByIdAsync(int id)
        {
            var entity = await _repo.GetComboWithItemsAsync(id);
            if (entity == null) throw new KeyNotFoundException("Combo not found");

            // Map Entity -> DTO
            return new ComboDetailDto
            {
                ComboId = entity.ComboId,
                Name = entity.Name,
                SellingPrice = entity.Price,
                Description = entity.Description,
                IsAvailable = (bool)entity.IsAvailable,
                ImageUrl = entity.ImageUrl,
                Items = entity.ComboItems.Select(ci => new ComboItemDto
                {
                    MenuItemId = ci.MenuItemId,
                    MenuItemName = ci.MenuItem.Name,
                    OriginalPrice = ci.MenuItem.Price,
                    Quantity = ci.Quantity,
                    ImageUrl = ci.MenuItem.ImageUrl,
                    CategoryName = ci.MenuItem.Category.CategoryName,
                }).ToList()
            };
        }

        public async Task<PagedResult<MenuItemDto>> SearchAsync(
     string? keyword,
     string? categoryName,
     int pageIndex)
        {
            const int pageSize = 9;
            pageIndex = pageIndex <= 0 ? 1 : pageIndex;

            IQueryable<MenuItem> query = _repo.QueryMenuItems();

            query = query.Include(x => x.Category).Where(a => a.IsAvailable == true);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(x => x.Name.Contains(keyword));

            if (!string.IsNullOrWhiteSpace(categoryName))
                query = query.Where(x => x.Category != null && x.Category.CategoryName == categoryName);

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.Name)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new MenuItemDto
                {
                    MenuItemId = x.MenuItemId,
                    MenuItemName = x.Name,
                    OriginalPrice = x.Price,
                    ImageURL = x.ImageUrl,
                    CategoryName = x.Category.CategoryName != null ? x.Category.CategoryName : null
                })
                .ToListAsync();

            return new PagedResult<MenuItemDto>(
                items,
                totalRecords,
                pageIndex,
                pageSize
            );
        }



        public async Task UpdateAsync(int id, UpdateComboDto request)
        {
            if (request.Items == null || !request.Items.Any())
                throw new ArgumentException("Combo phải có ít nhất 1 món.");

            if (request.Items.Count == 1 && request.Items[0].Quantity < 2)
                throw new ArgumentException("Combo chỉ có 1 món thì quantity phải ≥ 2.");

            foreach (var item in request.Items)
            {
                if (item.Quantity < 1)
                    throw new ArgumentException(
                        $"Item với MenuItemId {item.MenuItemId} phải có quantity ≥ 1.");
            }

            // Lấy combo kèm items
            var entity = await _repo.GetComboWithItemsAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Combo không tồn tại");

            // ====== 1. Kiểm tra combo đang được dùng trong order ======
            var inUse = entity.OrderDetails
                .Any(od => od.Status == "Ready" || od.Status == "Pending" || od.Status == "Cooking");

            if (inUse)
            {
                throw new InvalidOperationException(
                    "Không thể cập nhật combo vì combo đang được sử dụng trong các đơn hàng."
                );
            }

            // ====== 2. Kiểm tra nếu bật combo thì tất cả món phải available ======
            if (request.IsAvailable == true)
            {
                var menuItemIds = request.Items.Select(x => x.MenuItemId).Distinct().ToList();
                var menuItems = await _repo.GetMenuItemsByIdsAsync(menuItemIds);

                var unavailableItems = menuItems
                    .Where(x => x.IsAvailable != true) 
                    .ToList();

                if (unavailableItems.Any())
                {
                    var itemNames = string.Join(", ", unavailableItems.Select(x => x.Name));
                    throw new InvalidOperationException(
                        $"Không thể bật combo vì các món sau đang ngừng bán: {itemNames}"
                    );
                }
            }

            // ====== 3. Map header ======
            entity.Name = request.Name;
            entity.Price = request.SellingPrice;
            entity.Description = request.Description;
            entity.IsAvailable = request.IsAvailable;
            entity.ImageUrl = request.ImageUrl;

            // ====== 4. Map items ======
            var newItems = request.Items.Select(x => new ComboItem
            {
                MenuItemId = x.MenuItemId,
                Quantity = x.Quantity
            }).ToList();

            await _repo.UpdateComboAsync(entity, newItems);
        }



        Task<ComboDetailDto> IManagerComboService.GetComboByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<List<MenuItemDto>> GetTop5NewMenuItemsAsync()
        {
            var entities = await _repo.GetTop5NewMenuItemsAsync();

            var dtos = entities.Select(x => new MenuItemDto
            {
                MenuItemId = x.MenuItemId,
                MenuItemName = x.Name,
                OriginalPrice = x.Price,
                CategoryName = x.Category.CategoryName,
                ImageURL = x.ImageUrl,
            }).ToList();

            return dtos;
        }

    }
}
