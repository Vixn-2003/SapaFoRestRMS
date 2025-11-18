using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.DTOs.UserManagement
{
    public class UserCreateRequest
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên không được quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được quá 100 ký tự")]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được quá 20 ký tự")]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        [Range(1, 5, ErrorMessage = "Vai trò không hợp lệ")]
        [Display(Name = "Vai trò")]
        public int RoleId { get; set; }

        [Display(Name = "Trạng thái")]
        public int Status { get; set; } = 0; // Default to Active

        [Display(Name = "Mật khẩu tạm thời")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-50 ký tự")]
        public string? TemporaryPassword { get; set; }

        [Display(Name = "Gửi email thông báo")]
        public bool SendEmailNotification { get; set; } = true;
    }
}
