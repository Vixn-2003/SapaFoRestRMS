using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Positions
{
    /// <summary>
    /// DTO cho request tạo Position mới
    /// Lưu ý: Chỉ Owner/Admin mới có quyền tạo Position với BaseSalary
    /// Manager muốn thay đổi BaseSalary phải tạo SalaryChangeRequest
    /// </summary>
    public class PositionCreateRequest
    {
        [Required(ErrorMessage = "PositionName là bắt buộc")]
        [StringLength(100, ErrorMessage = "PositionName không được vượt quá 100 ký tự")]
        public string PositionName { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Description không được vượt quá 500 ký tự")]
        public string? Description { get; set; }

        [Range(0, 2, ErrorMessage = "Status phải từ 0 đến 2")]
        public int Status { get; set; } = 0;

        /// <summary>
        /// Lương cơ bản ban đầu (VND)
        /// Chỉ Owner/Admin mới có quyền set giá trị này khi tạo Position
        /// </summary>
        [Range(500000, int.MaxValue, ErrorMessage = "BaseSalary phải lớn hơn hoặc bằng 500000")]
        public int BaseSalary { get; set; } = 0;
    }
}

