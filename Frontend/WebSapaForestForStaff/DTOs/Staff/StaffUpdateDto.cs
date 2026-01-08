using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.DTOs.Staff
{
    /// <summary>
    /// DTO for updating existing staff
    /// </summary>
    public class StaffUpdateDto
    {
        [Required]
        public int StaffId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lương cơ bản")]
        [Range(0, double.MaxValue)]
        public decimal BaseSalary { get; set; }

        [Required]
        public int Status { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn chức vụ")]
        [Range(1, int.MaxValue, ErrorMessage = "A valid position must be selected")]
        public int PositionId { get; set; }

        public string? AvatarUrl { get; set; }
    }
}

