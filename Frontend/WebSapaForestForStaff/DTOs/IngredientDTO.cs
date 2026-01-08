using WebSapaForestForStaff.DTOs.Inventory;

namespace WebSapaForestForStaff.DTOs
{
    public class IngredientDTO
    {
        public int IngredientId { get; set; }
        public string IngredientCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? UnitId { get; set; }
        public decimal? ReorderLevel { get; set; }

        // ✅ QUAN TRỌNG: Phải có để deserialize nested Unit
        public UnitDTO Unit { get; set; } = new UnitDTO();

        // ✅ THÊM: Helper properties
        public string UnitName => Unit?.UnitName ?? "N/A";
    }
}
