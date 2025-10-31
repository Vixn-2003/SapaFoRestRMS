namespace WebSapaFoRestForCustomer.DTOs.OrderTable
{
    public class OrderDetailStatusViewModel
    {
        public int OrderDetailId { get; set; }
        public int MenuItemId { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? Notes { get; set; }
    }
}
