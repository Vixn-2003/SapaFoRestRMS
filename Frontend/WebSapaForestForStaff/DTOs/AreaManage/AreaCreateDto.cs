namespace WebSapaForestForStaff.DTOs.AreaManage
{
    public class AreaCreateDto
    {
        public string AreaName { get; set; } = null!;
        public int Floor { get; set; }
        public string? Description { get; set; }
    }
}
