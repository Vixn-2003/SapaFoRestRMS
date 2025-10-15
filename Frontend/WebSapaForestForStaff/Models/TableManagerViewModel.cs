namespace WebSapaForestForStaff.Models
{
    public class TableManagerViewModel
    {
        public int TableId { get; set; }
        public string TableName { get; set; } = null!;
        public int Capacity { get; set; }
        public string Status { get; set; } = "Available";
        public int AreaId { get; set; }
        public string AreaName { get; set; } = null!;
    }
}
