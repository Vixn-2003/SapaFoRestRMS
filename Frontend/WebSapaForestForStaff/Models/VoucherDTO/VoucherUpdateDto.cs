namespace WebSapaForestForStaff.Models.VoucherDTO
{
    public class VoucherUpdateDto
    {
        public string? Description { get; set; }
        public string? DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public decimal? MinOrderValue { get; set; }
        public decimal? MaxDiscount { get; set; }
        public string? Status { get; set; }
    }
}
