using System.ComponentModel.DataAnnotations;
using WebSapaForestForStaff.DTOs.Staff;

namespace WebSapaForestForStaff.Models.StaffViewModels
{
    /// <summary>
    /// ViewModel for Create Staff page
    /// </summary>
    public class StaffCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [Display(Name = "Full Name")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [Display(Name = "Email")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Phone")]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lương cơ bản")]
        [Display(Name = "Base Salary")]
        [Range(0, double.MaxValue, ErrorMessage = "Base salary must be positive")]
        public decimal BaseSalary { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày vào làm")]
        [Display(Name = "Hire Date")]
        public DateOnly HireDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn chức vụ")]
        [Display(Name = "Position")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a position")]
        public int PositionId { get; set; }

        [Display(Name = "Password (optional)")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string? Password { get; set; }

        [Display(Name = "Avatar URL")]
        public string? AvatarUrl { get; set; }

        [Display(Name = "Avatar File")]
        public IFormFile? AvatarFile { get; set; }

        // For dropdown
        public List<PositionDto> AvailablePositions { get; set; } = new();
    }
}

