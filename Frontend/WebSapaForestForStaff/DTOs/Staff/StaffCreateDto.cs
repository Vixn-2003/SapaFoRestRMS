using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.DTOs.Staff
{
    /// <summary>
    /// DTO for creating new staff
    /// </summary>
    public class StaffCreateDto
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Base salary is required")]
        [Range(0, double.MaxValue)]
        public decimal BaseSalary { get; set; }

        [Required(ErrorMessage = "Hire date is required")]
        public DateOnly HireDate { get; set; }

        [Required(ErrorMessage = "Department is required")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "At least one position is required")]
        [MinLength(1)]
        public List<int> PositionIds { get; set; } = new();

        [Required(ErrorMessage = "Role is required")]
        public int RoleId { get; set; }

        [StringLength(100, MinimumLength = 6)]
        public string? Password { get; set; }

        public string? AvatarUrl { get; set; }
    }
}

