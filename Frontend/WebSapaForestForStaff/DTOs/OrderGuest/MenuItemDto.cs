namespace WebSapaForestForStaff.DTOs.OrderGuest
{
    public class MenuItemDto
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public bool? IsAvailable { get; set; }
    }
}
