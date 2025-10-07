using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DomainAccessLayer.Models;

public partial class Voucher
{
    public int VoucherId { get; set; }

    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    public string? DiscountType { get; set; }

    public decimal DiscountValue { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public decimal? MinOrderValue { get; set; }

    public decimal? MaxDiscount { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]

    public string? Status { get; set; }

    public bool? IsDelete { get; set; }
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
