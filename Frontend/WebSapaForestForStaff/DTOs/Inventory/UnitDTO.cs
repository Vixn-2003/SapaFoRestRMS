namespace WebSapaForestForStaff.DTOs.Inventory
{
    public class UnitDTO
    {
        public int UnitId { get; set; }

        public string UnitName { get; set; } = null!;

        public UnitType UnitType { get; set; }
    }
}
