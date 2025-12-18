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
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(100, ErrorMessage = "Full name must not exceed 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100, ErrorMessage = "Email must not exceed 100 characters")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
        [StringLength(20, ErrorMessage = "Phone must not exceed 20 characters")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lương cơ bản")]
        [Range(0, double.MaxValue, ErrorMessage = "Base salary must be a positive number")]
        public decimal BaseSalary { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày vào làm")]
        public DateOnly HireDate { get; set; }

        /// <summary>
        /// Position ID to assign to staff (single position only)
        /// </summary>
        [Required(ErrorMessage = "Vui lòng chọn chức vụ")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn chức vụ hợp lệ")]
        public int PositionId { get; set; }

        /// <summary>
        /// Role ID for the user account (e.g., Waiter, Chef, etc.)
        /// </summary>
        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        public int RoleId { get; set; }

        /// <summary>
        /// Optional password - if not provided, system will generate one
        /// </summary>
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 100 ký tự")]
        public string? Password { get; set; }

        /// <summary>
        /// Avatar URL (optional)
        /// </summary>
        public string? AvatarUrl { get; set; }
    }
}

