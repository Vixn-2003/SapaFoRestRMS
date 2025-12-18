namespace WebSapaForestForStaff.DTOs.ManagementCombo
{
    public class UpdateComboDto
    {
        public string Name { get; set; }
        public decimal SellingPrice { get; set; }
        public string Description { get; set; }
        public bool IsAvailable { get; set; }
        public string ImageUrl { get; set; }
        public List<ComboItemInput> Items { get; set; } = new List<ComboItemInput>();
    }
}
