using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.DTOs.UserManagement
{
    public class PasswordResetRequest
    {
        [Required]
        public int UserId { get; set; }

        [Display(Name = "Gửi email thông báo")]
        public bool SendEmailNotification { get; set; } = true;

        [Display(Name = "Mật khẩu mới")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-50 ký tự")]
        public string? NewPassword { get; set; }
    }
}
