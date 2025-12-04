
using BusinessAccessLayer.DTOs.Inventory;

namespace WebSapaForestForStaff.DTOs
{
    public class RecipeDTO
    {
        public int RecipeId { get; set; }
        public int MenuItemId { get; set; }
        public int IngredientId { get; set; }
        public decimal QuantityNeeded { get; set; }

        public IngredientDTO Ingredient { get; set; } = new IngredientDTO();

        public string QuantityDisplay => $"{QuantityNeeded:F2} {Ingredient?.Unit?.UnitName ?? ""}";
    }
}
