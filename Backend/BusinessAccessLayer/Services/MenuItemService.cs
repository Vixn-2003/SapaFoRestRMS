using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class MenuItemService : IMenuItemService
    {
        private readonly IMenuItemRepository _menuItemRepository;

        public MenuItemService(IMenuItemRepository menuItemRepository)
        {
            _menuItemRepository = menuItemRepository;
        }

        public async Task<IEnumerable<BestSellerDto>> GetTopBestSellersAsync(int top = 10)
        {
            var data = await _menuItemRepository.GetTopBestSellersAsync(top);
            return data
      .Where(x => x.MenuItem != null) // lọc các item không có MenuItem
      .Select(x => new BestSellerDto
      {
          MenuItemId = x.MenuItem.MenuItemId,
          MenuItemName = x.MenuItem.Name,
          TotalQuantity = x.TotalQuantity,
          Description = x.MenuItem.Description,
          ImageUrl = x.MenuItem.ImageUrl,
          Price = x.MenuItem.Price
      });

        }
    }
}
