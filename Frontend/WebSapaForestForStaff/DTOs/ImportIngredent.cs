
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.DTOs.Inventory;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class ImportIngredient
    {
        public List<SupplierDTO> SupplierDTOs { get; set; }
        public List<InventoryIngredientDTO> InventoryIngredientDTOs { get; set; }

        public List<WarehouseDTO> WarehouseDTOs { get; set; } = new List<WarehouseDTO>();
        public List<PurchaseOrderDTO> PurchaseOrderDTOs { get; set; } = new List<PurchaseOrderDTO>();
        public List<UnitDTO> unitDTOs { get; set; } = new List<UnitDTO>();
    }


}
