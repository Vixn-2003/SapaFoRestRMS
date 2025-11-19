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

        [Required(ErrorMessage = "Vui lòng nhập mã xác minh được gửi tới email")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã xác minh gồm 6 ký tự")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã xác minh chỉ gồm 6 chữ số")]
        [Display(Name = "Mã xác minh")]
        public string VerificationCode { get; set; } = null!;

        public DateOnly? HireDate { get; set; }

        public decimal? SalaryBase { get; set; }

        [Display(Name = "Vị trí")]
        public List<int> PositionIds { get; set; } = new();

        [Display(Name = "Vai trò")]
        public int? RoleId { get; set; }
    }
}
