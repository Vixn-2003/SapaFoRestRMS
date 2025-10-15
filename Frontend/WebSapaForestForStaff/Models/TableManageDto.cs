namespace WebSapaForestForStaff.Models
{
    public class TableManageDto
    {
        public int TableId { get; set; }
        public string TableNumber { get; set; } = null!;
        public int Capacity { get; set; }
        public string Status { get; set; } = "Available";
        public int AreaId { get; set; }
        public string AreaName { get; set; } = null!;
    }
}
