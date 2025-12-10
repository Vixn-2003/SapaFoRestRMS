using BusinessAccessLayer.DTOs.ManagementCombo;
using BusinessAccessLayer.DTOs.Manager;
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

        Task UpdateComboAsync(int id, UpdateComboRequest request);
        Task<ComboDetailDto> GetByIdAsync(int id);
        Task<List<MenuItemDto>> SearchMenuAsync(string keyword);
        Task UpdateAsync(int id, UpdateComboDto request);
    }
}
