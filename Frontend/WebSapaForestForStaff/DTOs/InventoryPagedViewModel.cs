using BusinessAccessLayer.DTOs.Inventory;

namespace WebSapaForestForStaff.DTOs
{
    public class InventoryPagedViewModel
    {
        public List<InventoryIngredientDTO> Ingredients { get; set; } = new List<InventoryIngredientDTO>();
        public int CurrentPage { get; set; }
        public int ItemsPerPage { get; set; }
        public int TotalItems { get; set; }

        public string SearchIngredent { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
