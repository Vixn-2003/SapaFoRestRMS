namespace BusinessAccessLayer.DTOs.Positions
{
    public class PositionDto
    {
        public int PositionId { get; set; }
        public string PositionName { get; set; } = null!;
        public string? Description { get; set; }
        public int Status { get; set; }
    }
}

