using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BusinessAccessLayer.DTOs.Users
{
    public class UserUpdateRequest
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

        [Range(0, 2)]
        public int Status { get; set; }

        [StringLength(500)]
        public string? AvatarUrl { get; set; }

        // Tùy chọn upload file ảnh đại diện (ưu tiên dùng Cloudinary)
        public IFormFile? AvatarFile { get; set; }
    }
}

