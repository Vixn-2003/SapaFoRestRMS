using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.DTOs.Payment
{
    /// <summary>
    /// Request DTO cho việc hoàn tác xác nhận đơn hàng
    /// </summary>
    public class UndoConfirmRequest
    {
        [Required(ErrorMessage = "OrderId là bắt buộc")]
        public int OrderId { get; set; }

        [Required(ErrorMessage = "StaffId là bắt buộc")]
        public int StaffId { get; set; }

        [Required(ErrorMessage = "Lý do hoàn tác là bắt buộc")]
        [MinLength(10, ErrorMessage = "Lý do phải có ít nhất 10 ký tự")]
        public string Reason { get; set; } = string.Empty;
    }
}

