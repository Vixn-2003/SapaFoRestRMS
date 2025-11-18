namespace WebSapaFoRestForCustomer.DTOs.OrderTable
{
    public class MenuItemViewModel
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
        public string? ImageUrl { get; set; }

        // Số lượng đã gọi (từ API)
        public int Quantity { get; set; }
    }
}
