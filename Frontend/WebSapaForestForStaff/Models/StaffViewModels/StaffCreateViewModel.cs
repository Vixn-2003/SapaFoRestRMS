using System.ComponentModel.DataAnnotations;
using WebSapaForestForStaff.DTOs.Staff;

namespace WebSapaForestForStaff.Models.StaffViewModels
{
    /// <summary>
    /// ViewModel for Create Staff page
    /// </summary>
    public class StaffCreateViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone")]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Base salary is required")]
        [Display(Name = "Base Salary")]
        [Range(0, double.MaxValue, ErrorMessage = "Base salary must be positive")]
        public decimal BaseSalary { get; set; }

        [Required(ErrorMessage = "Hire date is required")]
        [Display(Name = "Hire Date")]
        public DateOnly HireDate { get; set; }

        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Position is required")]
        [Display(Name = "Position")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a position")]
        public int PositionId { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "Role")]
        public int RoleId { get; set; }

        [Display(Name = "Password (optional)")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string? Password { get; set; }

        [Display(Name = "Avatar URL")]
        public string? AvatarUrl { get; set; }

        // For dropdown
        public List<PositionDto> AvailablePositions { get; set; } = new();
    }
}

