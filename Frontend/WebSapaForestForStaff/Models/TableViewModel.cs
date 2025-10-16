namespace WebSapaForestForStaff.Models
{

    public class TableViewModel
    {
        public int TableId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int AreaId { get; set; }
        public string AreaName { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}
