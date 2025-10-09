using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.DTOs
{
    public class User
    {
        public int UserId { get; set; }

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

        [Display(Name = "Vai trò")]
        public int RoleId { get; set; }

        [Display(Name = "Trạng thái")]
        public int Status { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime? CreatedAt { get; set; }

        [Display(Name = "Người tạo")]
        public int? CreatedBy { get; set; }

        [Display(Name = "Ngày sửa")]
        public DateTime? ModifiedAt { get; set; }

        [Display(Name = "Người sửa")]
        public int? ModifiedBy { get; set; }

        [Display(Name = "Ngày xóa")]
        public DateTime? DeletedAt { get; set; }

        [Display(Name = "Người xóa")]
        public int? DeletedBy { get; set; }

        [Display(Name = "Đã xóa")]
        public bool? IsDeleted { get; set; }

        // Navigation properties
        [Display(Name = "Vai trò")]
        public string? RoleName { get; set; }
    }
}
