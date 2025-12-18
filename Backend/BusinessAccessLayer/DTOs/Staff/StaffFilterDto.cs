namespace BusinessAccessLayer.DTOs.Staff
{
    /// <summary>
    /// DTO for filtering and sorting staff list
    /// Used in UC55 - View List Staff
    /// </summary>
    public class StaffFilterDto
    {
        /// <summary>
        /// Search by name, phone, or email
        /// </summary>
        public string? SearchKeyword { get; set; }

        /// <summary>
        /// Filter by position name
        /// </summary>
        public string? Position { get; set; }

        /// <summary>
        /// Filter by status (0=Inactive, 1=Active, etc.)
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        /// Department ID to filter staff
        /// </summary>
        public int? DepartmentId { get; set; }

        /// <summary>
        /// Sort field: "Name", "Position", "BaseSalary", "HireDate"
        /// </summary>
        public string SortBy { get; set; } = "HireDate";

        /// <summary>
        /// Sort direction: "asc" or "desc"
        /// </summary>
        public string SortDirection { get; set; } = "desc";

        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Items per page
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}

