using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.DTOs.UserManagement
{
    public class CreateStaffRequest
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

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được quá 20 ký tự")]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Ngày tuyển dụng là bắt buộc")]
        [Display(Name = "Ngày tuyển dụng")]
        public DateOnly HireDate { get; set; }

        [Required(ErrorMessage = "Lương cơ bản là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Lương cơ bản phải lớn hơn 0")]
        [Display(Name = "Lương cơ bản")]
        public decimal SalaryBase { get; set; }

        [Display(Name = "Vị trí")]
        public List<int> PositionIds { get; set; } = new();

        [Display(Name = "Vai trò")]
        public int? RoleId { get; set; }
    }
}
