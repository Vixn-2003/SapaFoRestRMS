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
        /// Position ID to assign to staff (single position only)
        /// </summary>
        [Required(ErrorMessage = "Position is required")]
        [Range(1, int.MaxValue, ErrorMessage = "A valid position must be selected")]
        public int PositionId { get; set; }

        /// <summary>
        /// Avatar URL (optional)
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Note: Email, Department, and HireDate cannot be changed
        /// </summary>
    }
}

