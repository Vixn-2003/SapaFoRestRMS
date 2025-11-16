using BusinessAccessLayer.DTOs.Inventory;
using WebSapaForestForStaff.DTOs;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class ImportIngredient
    {
        public List<SupplierDTO> SupplierDTOs { get; set; }
        public List<InventoryIngredientDTO> InventoryIngredientDTOs { get; set; }

        public List<WarehouseDTO> WarehouseDTOs { get; set; } = new List<WarehouseDTO>();
        public List<PurchaseOrderDTO> PurchaseOrderDTOs { get; set; } = new List<PurchaseOrderDTO>();
    }


}
