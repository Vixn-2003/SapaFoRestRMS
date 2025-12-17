using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Staff
{
    /// <summary>
    /// DTO for creating new staff
    /// Used in Create Staff UC
    /// </summary>
    public class StaffCreateDto
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name must not exceed 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email must not exceed 100 characters")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20, ErrorMessage = "Phone must not exceed 20 characters")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Base salary is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Base salary must be a positive number")]
        public decimal BaseSalary { get; set; }

        [Required(ErrorMessage = "Hire date is required")]
        public DateOnly HireDate { get; set; }

        /// <summary>
        /// Department ID - Manager can only assign to their department
        /// </summary>
        [Required(ErrorMessage = "Department is required")]
        public int DepartmentId { get; set; }

        /// <summary>
        /// Position ID to assign to staff (single position only)
        /// </summary>
        [Required(ErrorMessage = "Position is required")]
        [Range(1, int.MaxValue, ErrorMessage = "A valid position must be selected")]
        public int PositionId { get; set; }

        /// <summary>
        /// Role ID for the user account (e.g., Waiter, Chef, etc.)
        /// </summary>
        [Required(ErrorMessage = "Role is required")]
        public int RoleId { get; set; }

        /// <summary>
        /// Optional password - if not provided, system will generate one
        /// </summary>
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        public string? Password { get; set; }

        /// <summary>
        /// Avatar URL (optional)
        /// </summary>
        public string? AvatarUrl { get; set; }
    }
}

