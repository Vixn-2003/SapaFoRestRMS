namespace WebSapaForestForStaff.DTOs.Staff
{
    /// <summary>
    /// DTO for staff list item display
    /// Used in UC55 - View List Staff
    /// </summary>
    public class StaffListItemDto
    {
        public int StaffId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Positions { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public DateOnly HireDate { get; set; }
        public string? DepartmentName { get; set; }
        public int? DepartmentId { get; set; }
    }
}

