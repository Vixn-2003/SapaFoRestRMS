namespace WebSapaForestForStaff.DTOs.AreaManage
{
    public class AreaUpdateDto
    {
        public int AreaId { get; set; }
        public string AreaName { get; set; } = null!;
        public int Floor { get; set; }
        public string? Description { get; set; }
    }
}
