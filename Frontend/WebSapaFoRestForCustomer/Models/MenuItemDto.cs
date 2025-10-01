namespace WebSapaFoRestForCustomer.Models
{
    public class MenuItemDto
    {
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; } = null!;
        public int TotalQuantity { get; set; }

        public string? Description { get; set; }   // Mô tả món
        public string? ImageUrl { get; set; }

        public decimal Price { get; set; }
    }
}
