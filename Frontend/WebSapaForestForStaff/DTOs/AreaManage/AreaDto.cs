namespace WebSapaForestForStaff.DTOs.AreaManage
{
    public class AreaDto
    {
        public int AreaId { get; set; }
        public string AreaName { get; set; } = null!;
        public int Floor { get; set; }
        public string? Description { get; set; }
    }
}
