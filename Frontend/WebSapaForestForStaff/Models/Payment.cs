using System;
using System.Collections.Generic;

namespace WebSapaForestForStaff.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public decimal Subtotal { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal? Vatpercent { get; set; }

    public decimal? Vatamount { get; set; }

    public decimal FinalAmount { get; set; }

    public int? VoucherId { get; set; }

    public DateTime? PaymentDate { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Voucher? Voucher { get; set; }
}
