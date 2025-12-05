using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.DTOs.ManagementCombo;
using BusinessAccessLayer.DTOs.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
