using System.ComponentModel.DataAnnotations;
using WebSapaForestForStaff.DTOs.Staff;

namespace WebSapaForestForStaff.Models.StaffViewModels
{
    /// <summary>
    /// ViewModel for Edit Staff page
    /// UC56 - Update Staff
    /// </summary>
    public class StaffEditViewModel
    {
        public int StaffId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty; // Read-only, cannot change

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone")]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Base salary is required")]
        [Display(Name = "Base Salary")]
        [Range(0, double.MaxValue, ErrorMessage = "Base salary must be positive")]
        public decimal BaseSalary { get; set; }

        [Required]
        [Display(Name = "Status")]
        public int Status { get; set; }

        [Display(Name = "Hire Date")]
        public DateOnly HireDate { get; set; } // Read-only, cannot change

        [Display(Name = "Department")]
        public string? DepartmentName { get; set; } // Read-only, cannot change

        [Required(ErrorMessage = "Position is required")]
        [Display(Name = "Position")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a position")]
        public int PositionId { get; set; }

        [Display(Name = "Avatar URL")]
        public string? AvatarUrl { get; set; }

        // For dropdown
        public List<PositionDto> AvailablePositions { get; set; } = new();
    }
}

