using WebSapaFoRestForCustomer.Models;

namespace WebSapaFoRestForCustomer.DTOs.OrderTable
{
    public class MenuPageViewModel
    {
        public IEnumerable<MenuItemDto> MenuItems { get; set; } = new List<MenuItemDto>();
        public List<OrderDetailStatusViewModel> OrderedItems { get; set; } = new List<OrderDetailStatusViewModel>();
        public IEnumerable<ComboOrderDto> Combos { get; set; } // <-- THÊM DÒNG NÀY
        public string TableNumber { get; set; }
        public string AreaName { get; set; }
        public int? Floor { get; set; }
    }
}
