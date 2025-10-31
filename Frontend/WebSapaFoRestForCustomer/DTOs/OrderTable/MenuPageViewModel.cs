namespace WebSapaFoRestForCustomer.DTOs.OrderTable
{
    public class MenuPageViewModel
    {
        public List<MenuItemViewModel> MenuItems { get; set; } = new();
        public List<OrderDetailStatusViewModel> OrderedItems { get; set; } = new();
            // === THÊM CÁC DÒNG NÀY ===
        public string TableNumber { get; set; }
        public string AreaName { get; set; }
        public int? Floor { get; set; }
    }
}
