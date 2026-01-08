using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.DTOs.UserManagement
{
    public class CreateStaffVerificationRequest
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
    }
}


