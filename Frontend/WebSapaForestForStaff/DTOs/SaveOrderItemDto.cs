namespace WebSapaForestForStaff.DTOs
{
    public class SaveOrderItemDto
    {
        public int OrderItemId { get; set; } // 0 nếu là món mới
        public int? MenuItemId { get; set; }
        public int? ComboId { get; set; }
        public int Quantity { get; set; }
        public string Note { get; set; }
    }
}
