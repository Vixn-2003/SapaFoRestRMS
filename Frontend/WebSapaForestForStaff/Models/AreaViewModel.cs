namespace WebSapaForestForStaff.Models
{
    public class AreaViewModel
    {
        public int AreaId { get; set; }
        public string AreaName { get; set; } = string.Empty;
        public List<TableViewModel> Tables { get; set; } = new();
    }
}
