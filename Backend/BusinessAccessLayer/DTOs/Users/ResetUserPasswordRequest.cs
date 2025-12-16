using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Users
{
    public class ResetUserPasswordRequest
    {
        [Required]
        public bool SendEmailNotification { get; set; } = true;

        [StringLength(50, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-50 ký tự")]
        public string? NewPassword { get; set; }
    }
}


