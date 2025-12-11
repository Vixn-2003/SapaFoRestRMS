namespace WebSapaForestForStaff.DTOs.Staff
{
    /// <summary>
    /// DTO for filtering and sorting staff list
    /// Used in UC55 - View List Staff
    /// </summary>
    public class StaffFilterDto
    {
        public string? SearchKeyword { get; set; }
        public string? Position { get; set; }
        public int? Status { get; set; }
        public int? DepartmentId { get; set; }
        public string SortBy { get; set; } = "HireDate";
        public string SortDirection { get; set; } = "desc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

