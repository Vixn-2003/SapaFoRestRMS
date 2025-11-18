namespace WebSapaForestForStaff.DTOs
{
    public class Position
    {
        public int PositionId { get; set; }
        public string PositionName { get; set; } = null!;
        public string? Description { get; set; }
        public int Status { get; set; }
    }
}
