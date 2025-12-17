using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BusinessAccessLayer.DTOs.Customers
{
    public class CustomerProfileUpdateRequest
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string FullName { get; set; } = null!;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        public string? Phone { get; set; }

        [StringLength(1000, ErrorMessage = "URL avatar không hợp lệ")]
        public string? AvatarUrl { get; set; }

        // Tùy chọn upload file ảnh đại diện
        public IFormFile? AvatarFile { get; set; }
    }
}
