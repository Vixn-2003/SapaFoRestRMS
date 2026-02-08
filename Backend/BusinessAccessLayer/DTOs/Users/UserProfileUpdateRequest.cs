using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BusinessAccessLayer.DTOs.Users
{
    public class UserProfileUpdateRequest
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = null!;

        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? AvatarUrl { get; set; }

        // Tùy chọn upload file ảnh đại diện (ưu tiên dùng Cloudinary)
        public IFormFile? AvatarFile { get; set; }
    }
}

