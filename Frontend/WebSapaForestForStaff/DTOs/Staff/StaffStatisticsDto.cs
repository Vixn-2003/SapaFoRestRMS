namespace WebSapaForestForStaff.DTOs.Staff
{
    /// <summary>
    /// DTO for staff statistics data
    /// Used for manager dashboard statistics
    /// </summary>
    public class StaffStatisticsDto
    {
        /// <summary>
        /// Total number of staff
        /// </summary>
        public int TotalStaff { get; set; }

        /// <summary>
        /// Number of active staff
        /// </summary>
        public int ActiveStaff { get; set; }

        /// <summary>
        /// Number of inactive staff
        /// </summary>
        public int InactiveStaff { get; set; }

        /// <summary>
        /// Average base salary across all staff
        /// </summary>
        public decimal AverageSalary { get; set; }

        /// <summary>
        /// Total base salary sum across all staff
        /// </summary>
        public decimal TotalSalary { get; set; }

        /// <summary>
        /// Number of staff hired in the last 30 days
        /// </summary>
        public int RecentHires { get; set; }

        /// <summary>
        /// Staff count by department
        /// </summary>
        public List<DepartmentStatisticsDto> DepartmentStatistics { get; set; } = new();

        /// <summary>
        /// Staff count by position
        /// </summary>
        public List<PositionStatisticsDto> PositionStatistics { get; set; } = new();
    }

    /// <summary>
    /// Department statistics
    /// </summary>
    public class DepartmentStatisticsDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int TotalStaff { get; set; }
        public int ActiveStaff { get; set; }
        public int InactiveStaff { get; set; }
    }

    /// <summary>
    /// Position statistics
    /// </summary>
    public class PositionStatisticsDto
    {
        public int PositionId { get; set; }
        public string PositionName { get; set; } = string.Empty;
        public int TotalStaff { get; set; }
        public int ActiveStaff { get; set; }
        public int InactiveStaff { get; set; }
    }
}
