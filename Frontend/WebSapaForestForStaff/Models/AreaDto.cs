namespace WebSapaForestForStaff.Models
{
    public class AreaDto
    {
        public int AreaId { get; set; }
        public string AreaName { get; set; } = string.Empty;
        public int Floor { get; set; }
        public string? Description { get; set; }
    }

}
