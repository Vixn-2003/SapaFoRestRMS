using AutoMapper;
using AutoMapper.Configuration.Annotations;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class ManagerMenuService : IManagerMenuService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ManagerMenuService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<bool> AddRecipe(RecipeDTO dto)
        {
            var replaceDto = _mapper.Map<Recipe>(dto);
            var menu = await _unitOfWork.MenuItem.AddRecipe(replaceDto);
            return menu;
        }

        public async Task<bool> DeleteRecipeByMenuItemId(int menuItemId)
        {
            var menu = await _unitOfWork.MenuItem.DeleteRecipeByMenuItemId(menuItemId);
            return menu;
        }

        public async Task<IEnumerable<ManagerMenuDTO>> GetManagerAllMenu()
        {
            var menu = await _unitOfWork.MenuItem.GetManagerAllMenus();
            return _mapper.Map<IEnumerable<ManagerMenuDTO>>(menu);
        }

        public async Task<IEnumerable<RecipeDTO>> GetRecipeByMenuItem(int id)
        {
            var recipe = await _unitOfWork.MenuItem.GetRecipeByMenuItem(id);
            return _mapper.Map<IEnumerable<RecipeDTO>>(recipe);
        }

        public async Task<ManagerMenuDTO> ManagerMenuById(int id)
        {
            var menu = await _unitOfWork.MenuItem.ManagerMenuByIds(id);
            return _mapper.Map<ManagerMenuDTO>(menu);
        }

        public async Task<bool> UpdateManagerMenu(ManagerMenuDTO formUpdateMenuDTO)
        {
            if (formUpdateMenuDTO == null)
                throw new ArgumentNullException(nameof(formUpdateMenuDTO), "Dữ liệu cập nhật không được để trống.");

            try
            {               
                if (formUpdateMenuDTO.IsAvailable == false)
                {
                    var listCombo = await _unitOfWork.Combo.GetManagerAllCombos();
                    foreach (var combo in listCombo)
                    {
                        foreach(var c in combo.ComboItems)
                        {
                            if(c.MenuItemId == formUpdateMenuDTO.MenuItemId)
                            {
                                await _unitOfWork.Combo.ChangeStatusComboAsync(c.ComboId, false);
                            }
                        }
                    }
                }

                var mapping = _mapper.Map<MenuItem>(formUpdateMenuDTO);
                var result = await _unitOfWork.MenuItem.ManagerUpdateMenu(mapping);

                return result; // trả về kết quả thực tế (true/false)
            }
            catch (AutoMapperMappingException mapEx)
            {
                // Lỗi trong quá trình ánh xạ (mapping)
                Console.Error.WriteLine($"[Mapping Error] {mapEx.Message}");
                return false;
            }
            catch (DbUpdateException dbEx)
            {
                // Lỗi khi cập nhật cơ sở dữ liệu
                Console.Error.WriteLine($"[Database Error] {dbEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // Lỗi không xác định
                Console.Error.WriteLine($"[Unexpected Error] {ex.Message}");
                return false;
            }
        }


        public async Task<List<MenuItemStatisticsDto>> GetMenuItemsWithStatisticsAsync()
        {
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);
            var last7Days = today.AddDays(-7);
            var last30Days = today.AddDays(-30);
            var last90Days = today.AddDays(-90);

            // Lấy danh sách món ăn kèm OrderDetails từ 90 ngày trước
            var menuItems = await _unitOfWork.MenuItem.GetAllMenuItemsWithOrderDetailsAsync(last90Days);

            // Xử lý và mapping sang DTO
            var result = menuItems.Select(item =>
                MapToStatisticsDto(item, today, yesterday, last7Days, last30Days))
                .OrderByDescending(x => x.ServedToday)
                .ToList();

            return result;
        }

        private MenuItemStatisticsDto MapToStatisticsDto(
            MenuItem item,
            DateTime today,
            DateTime yesterday,
            DateTime last7Days,
            DateTime last30Days)
        {
            // OrderDetails đã được load sẵn trong item.OrderDetails
            var orderDetails = item.OrderDetails;

            // Tính số lượng đã bán
            var servedToday = orderDetails
                .Where(od => od.CreatedAt.Date == today)
                .Sum(od => od.Quantity);

            var servedYesterday = orderDetails
                .Where(od => od.CreatedAt.Date == yesterday)
                .Sum(od => od.Quantity);

            var served7Days = orderDetails
                .Where(od => od.CreatedAt.Date >= last7Days && od.CreatedAt.Date < today)
                .Sum(od => od.Quantity);
            var avg7Days = served7Days / 7.0;

            var served30Days = orderDetails
                .Where(od => od.CreatedAt.Date >= last30Days && od.CreatedAt.Date < today)
                .Sum(od => od.Quantity);
            var avg30Days = served30Days / 30.0;

            // Map sang DTO
            var dto = _mapper.Map<MenuItemStatisticsDto>(item);

            // Gán thống kê
            dto.ServedToday = servedToday;
            dto.ServedYesterday = servedYesterday;
            dto.Average7Days = Math.Round(avg7Days, 1);
            dto.Average30Days = Math.Round(avg30Days, 1);
            dto.Average90Days = 0; // Có thể tính thêm nếu cần

            // Tính % so sánh
            dto.CompareWithYesterday = CalculatePercentChange(servedToday, servedYesterday);
            dto.CompareWith7Days = CalculatePercentChange(servedToday, avg7Days);
            dto.CompareWith30Days = CalculatePercentChange(servedToday, avg30Days);

            return dto;
        }

        private double CalculatePercentChange(int current, double previous)
        {
            if (previous == 0)
                return current > 0 ? 100 : 0;

            return Math.Round(((current - previous) / previous) * 100, 1);
        }

        public async Task<int> CreateManagerMenu(ManagerMenuDTO menuDTO)
        {
            try
            {
                // Tạo entity từ DTO
                var menuItem = new MenuItem
                {
                    Name = menuDTO.Name,
                    CategoryId = menuDTO.CategoryId,
                    Price = menuDTO.Price,
                    IsAvailable = menuDTO.IsAvailable,
                    CourseType = menuDTO.CourseType,
                    Description = menuDTO.Description,
                    ImageUrl = menuDTO.ImageUrl,
                    TimeCook = menuDTO.TimeCook,
                    BillingType = menuDTO.BillingType,
                    IsAds = menuDTO.IsAds
                };

                // Thêm vào database
                await _unitOfWork.MenuItem.CreateManagerMenuRe(menuItem);
                await _unitOfWork.SaveChangesAsync();

                // Trả về ID vừa tạo
                return menuItem.MenuItemId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating menu: {ex.Message}");
                throw;
            }
        }

        public async Task<ManagerMenuDTO?> GetMenuByName(string name)
        {
            try
            {
                var menuItem = await _unitOfWork.MenuItem.GetMenuByNameRe(name);

                if (menuItem == null) return null;

                return new ManagerMenuDTO
                {
                    MenuItemId = menuItem.MenuItemId,
                    Name = menuItem.Name,
                    CategoryId = menuItem.CategoryId,
                    Price = menuItem.Price,
                    IsAvailable = menuItem.IsAvailable,
                    CourseType = menuItem.CourseType,
                    Description = menuItem.Description,
                    ImageUrl = menuItem.ImageUrl,
                    TimeCook = menuItem.TimeCook,
                    BillingType = menuItem.BillingType,
                    IsAds = menuItem.IsAds
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting menu by name: {ex.Message}");
                return null;
            }
        }
    }
}
