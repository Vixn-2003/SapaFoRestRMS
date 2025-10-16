using System;
using System.Collections.Generic;
using WebSapaForestForStaff.Models;

namespace WebSapaForestForStaff.Models
{
    public partial class Voucher
    {
        public int VoucherId { get; set; }
        public string Code { get; set; } = null!;
        public string? Description { get; set; }
        public string? DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public decimal? MinOrderValue { get; set; }
        public decimal? MaxDiscount { get; set; }
        public string? Status { get; set; }

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<MarketingCampaign> MarketingCampaigns { get; set; } = new List<MarketingCampaign>(); // Navigation ngược
    }
}