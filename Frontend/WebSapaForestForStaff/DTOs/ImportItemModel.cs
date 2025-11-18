namespace WebSapaForestForStaff.DTOs
{
    public class ImportItemModel
    {
        public int? IngredientId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Unit { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string WarehouseName { get; set; }
    }
}
