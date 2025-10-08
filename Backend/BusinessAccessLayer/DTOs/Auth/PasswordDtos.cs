using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Auth
{
    public class RequestPasswordResetDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }

    public class VerifyPasswordResetDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string Code { get; set; } = null!;
    }

    public class RequestChangePasswordDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string CurrentPassword { get; set; } = null!;
    }

    public class VerifyChangePasswordDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string Code { get; set; } = null!;

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = null!;
    }
}


