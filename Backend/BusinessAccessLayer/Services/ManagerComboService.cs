using AutoMapper;
using BusinessAccessLayer.DTOs.ManagementCombo;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using static BusinessAccessLayer.Services.OrderTableService;

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
                    CategoryName =comboItem.MenuItem.Category.CategoryName
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
        public async Task UpdateComboAsync(int id, UpdateComboRequest request)
        {
            // 1. Kiểm tra Combo có tồn tại không
            var existingCombo = await _repo.GetComboByIdWithItemsAsync(id);
            if (existingCombo == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy Combo với ID {id}");
            }

            // 2. Cập nhật thông tin cơ bản (Header)
            existingCombo.Name = request.Name;
            existingCombo.Price = request.ActualPrice;
            existingCombo.Description = request.Description;
            existingCombo.ImageUrl = request.ImageUrl;
            existingCombo.IsAvailable = request.IsAvailable;
            // existingCombo.UpdatedAt = DateTime.Now; // Nếu có trường này

            // 3. Chuẩn bị danh sách Items mới (Entity List)
            var newComboItems = new List<ComboItem>();

            // Optional: Validate xem các MenuItemId gửi lên có tồn tại trong DB không
            // var validMenuIds = await _menuItemRepo.GetActiveIdsAsync(...);

            foreach (var itemDto in request.Items)
            {
                newComboItems.Add(new ComboItem
                {
                    MenuItemId = itemDto.MenuItemId,
                    Quantity = itemDto.Quantity
                    // ComboId sẽ được gán trong Repo
                });
            }

            // 4. Gọi Repo để thực hiện lưu xuống DB
            await _repo.UpdateComboAsync(existingCombo, newComboItems);
        }
    }
}
