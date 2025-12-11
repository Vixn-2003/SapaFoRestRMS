using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Staff
{
    /// <summary>
    /// DTO for updating existing staff
    /// Used in UC56 - Update Staff
    /// </summary>
    public class StaffUpdateDto
    {
        [Required]
        public int StaffId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name must not exceed 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20, ErrorMessage = "Phone must not exceed 20 characters")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Base salary is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Base salary must be a positive number")]
        public decimal BaseSalary { get; set; }

        /// <summary>
        /// Status: 0=Inactive, 1=Active
        /// </summary>
        [Required]
        public int Status { get; set; }

        /// <summary>
        /// Position IDs to assign to staff
        /// </summary>
        [Required(ErrorMessage = "At least one position is required")]
        [MinLength(1, ErrorMessage = "At least one position must be selected")]
        public List<int> PositionIds { get; set; } = new();

        /// <summary>
        /// Avatar URL (optional)
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Note: Email, Department, and HireDate cannot be changed
        /// </summary>
    }
}

