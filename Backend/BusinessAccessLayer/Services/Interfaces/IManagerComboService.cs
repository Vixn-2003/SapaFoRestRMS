using BusinessAccessLayer.DTOs.ManagementCombo;
using BusinessAccessLayer.DTOs.Manager;
using DomainAccessLayer.Models;
using static BusinessAccessLayer.DTOs.ManagementCombo.UpdateDtosCombo;
using ComboDetailDto = BusinessAccessLayer.DTOs.ManagementCombo.UpdateDtosCombo.ComboDetailDto;
using MenuItemDto = BusinessAccessLayer.DTOs.ManagementCombo.UpdateDtosCombo.MenuItemDto;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IManagerComboService
    {
        Task<IEnumerable<ManagerComboDTO>> GetManagerAllCombo();
        Task<DTOs.ManagementCombo.PagedResult<DTOs.ManagementCombo.MenuItemDto>> GetMenuItemsAsync(MenuFilterRequest request);

        Task CreateComboAsync(CreateComboRequest request);

        Task<List<TopSellerDto>> GetTopSellersAsync(string type);

        Task<List<StatsDto>> GetComboSalesStatsAsync(string type);

   
        DTOs.ManagementCombo.PagedResult<ComboDisplayDto> GetComboDisplayList(string? search,
      bool? isAvailable,
      int pageIndex,
      int pageSize
  );
        Task<ComboDetailDto> GetComboByIdAsync(int id);

        Task<ComboDetailDto> GetByIdAsync(int id);
        Task<PagedResult<MenuItemDto>> SearchAsync(
                string? keyword,
                string? categoryName,
                int pageIndex);
        Task UpdateAsync(int id, UpdateComboDto request);
        Task AddComboAsync(CreateComboDto request);

        Task<List<MenuItemDto>> GetTop5NewMenuItemsAsync();

    }
}
