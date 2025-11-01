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

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = null!;

        [Range(0, 2)]
        public int Status { get; set; } = 0;
    }
}

