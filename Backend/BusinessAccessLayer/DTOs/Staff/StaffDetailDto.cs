using System;
using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.Staff
{
    /// <summary>
    /// DTO for staff detail view
    /// Used when viewing/editing staff details
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
        
        /// <summary>
        /// Department information
        /// </summary>
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        
        /// <summary>
        /// List of position IDs and names
        /// </summary>
        public List<StaffPositionDto> Positions { get; set; } = new();
        
        /// <summary>
        /// User account information
        /// </summary>
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        
        public DateTime? CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    /// <summary>
    /// Position information for staff
    /// </summary>
    public class StaffPositionDto
    {
        public int PositionId { get; set; }
        public string PositionName { get; set; } = string.Empty;
    }
}

