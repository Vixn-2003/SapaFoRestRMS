namespace WebSapaFoRestForCustomer.DTOs.OrderTable
{
    public class OrderDetailStatusViewModel
    {
        public int OrderDetailId { get; set; }
        public int? MenuItemId { get; set; }
        public int? ComboId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string? Notes { get; set; }
    }
}
