namespace WebSapaForestForStaff.DTOs.Staff
{
    /// <summary>
    /// DTO for position (matches backend PositionDto)
    /// </summary>
    public class PositionDto
    {
        public int PositionId { get; set; }
        public string PositionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Status { get; set; }
        public decimal BaseSalary { get; set; }
    }
}

