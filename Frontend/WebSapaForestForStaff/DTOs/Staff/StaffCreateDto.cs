using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.DTOs.Staff
{
    /// <summary>
    /// DTO for creating new staff
    /// </summary>
    public class StaffCreateDto
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lương cơ bản")]
        [Range(0, double.MaxValue)]
        public decimal BaseSalary { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày vào làm")]
        public DateOnly HireDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn chức vụ")]
        [Range(1, int.MaxValue, ErrorMessage = "A valid position must be selected")]
        public int PositionId { get; set; }

        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        public int RoleId { get; set; }

        [StringLength(100, MinimumLength = 6)]
        public string? Password { get; set; }

        public string? AvatarUrl { get; set; }
    }
}

