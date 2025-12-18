namespace WebSapaForestForStaff.DTOs.Inventory
{
    public class WarehouseDTO
    {
        public int WarehouseId { get; set; }

        public string Name { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public int BatchCount { get; set; }
    }
}
