using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.Models.VoucherDTO
{
    public class VoucherUpdateDto
    {
        public string? Description { get; set; }
        public string? DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? MinOrderValue { get; set; }
        public decimal? MaxDiscount { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
        public string? Status { get; set; }
    }
}
