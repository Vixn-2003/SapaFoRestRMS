using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.DTOs
{
    public class Position
    {
        public int PositionId { get; set; }

        [Required(ErrorMessage = "Tên vị trí là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên vị trí không được quá 100 ký tự")]
        [Display(Name = "Tên vị trí")]
        public string PositionName { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Mô tả không được quá 500 ký tự")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Trạng thái")]
        public int Status { get; set; }
    }
}
