namespace WebSapaFoRestForCustomer.DTOs.OrderTable
{
    public class ComboOrderDto
    {
        public int ComboId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; } // Đây là giá bán (đã giảm) của combo
        public string ImageUrl { get; set; }
        public bool IsAvailable { get; set; }

        public decimal OriginalPrice { get; set; }
    }
}
