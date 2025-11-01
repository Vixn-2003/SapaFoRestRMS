using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class VoucherCreateDto
    {
        [Required]
        public string Code { get; set; }

        public string Description { get; set; }

        [Required]
        public string DiscountType { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Discount value must be greater than 0")]
        public decimal DiscountValue { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Min order value must be >= 0")]
        public decimal? MinOrderValue { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Max discount must be >= 0")]
        public decimal? MaxDiscount { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
        public string? Status { get; set; }
    }
}
