using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.DTOs.Staff
{
    /// <summary>
    /// DTO for updating existing staff
    /// </summary>
    public class StaffUpdateDto
    {
        [Required]
        public int StaffId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Base salary is required")]
        [Range(0, double.MaxValue)]
        public decimal BaseSalary { get; set; }

        [Required]
        public int Status { get; set; }

        [Required(ErrorMessage = "At least one position is required")]
        [MinLength(1)]
        public List<int> PositionIds { get; set; } = new();

        public string? AvatarUrl { get; set; }
    }
}

