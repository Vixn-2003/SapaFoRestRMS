using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Users
{
    public class UserCreateRequest
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = null!;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = null!;

        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Required]
        [Range(1, 5)]
        public int RoleId { get; set; }

        // Cho phép bỏ trống để hệ thống tự tạo mật khẩu ngẫu nhiên
        [StringLength(100, MinimumLength = 6)]
        public string? Password { get; set; }

        [StringLength(50, MinimumLength = 6)]
        public string? TemporaryPassword { get; set; }

        public bool SendEmailNotification { get; set; } = true;

        [Range(0, 2)]
        public int Status { get; set; } = 0;
    }
}

