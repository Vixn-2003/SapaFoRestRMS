namespace WebSapaForestForStaff.DTOs.Staff
{
    /// <summary>
    /// DTO for staff detail view
    /// </summary>
    public class StaffDetailDto
    {
        public int StaffId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public DateOnly HireDate { get; set; }
        public decimal BaseSalary { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public List<StaffPositionDto> Positions { get; set; } = new();
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class StaffPositionDto
    {
        public int PositionId { get; set; }
        public string PositionName { get; set; } = string.Empty;
    }
}

