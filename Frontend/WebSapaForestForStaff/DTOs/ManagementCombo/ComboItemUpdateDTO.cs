namespace WebSapaForestForStaff.DTOs.ManagementCombo
{
    public class ComboItemUpdateDTO
    {
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; } 
        public decimal OriginalPrice { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }
        public string CategoryName { get; set; }
    }
}
