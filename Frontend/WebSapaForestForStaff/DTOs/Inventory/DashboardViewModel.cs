using BusinessAccessLayer.DTOs.Inventory;

namespace WebSapaForestForStaff.DTOs.Inventory
{
    public class DashboardViewModel
    {
        public List<InventoryIngredientDTO> Ingredients { get; set; } = new List<InventoryIngredientDTO>();
        public List<PurchaseOrderDTO> PurchaseOrders { get; set; } = new List<PurchaseOrderDTO>();
        public List<AuditInventoryDTO> AuditInventories { get; set; } = new List<AuditInventoryDTO>();
    }
}
