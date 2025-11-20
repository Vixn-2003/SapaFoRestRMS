using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainAccessLayer.Models
{
    public class ReservationDeposit
    {
        [Key]
        public int DepositId { get; set; }

        [ForeignKey(nameof(Reservation))]
        public int ReservationId { get; set; }

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required, MaxLength(50)]
        public string PaymentMethod { get; set; } = null!; // Ví dụ: "Chuyển khoản", "Tiền mặt", "Momo"

        [Required, MaxLength(100)]
        public string DepositCode { get; set; } = null!; // Mã giao dịch / mã phiếu thu

        public DateTime DepositDate { get; set; } = DateTime.Now;

        public string? Notes { get; set; }

        //  Ảnh biên lai chuyển khoản
        public string? ReceiptImageUrl { get; set; }

        // Navigation
        public virtual Reservation Reservation { get; set; } = null!;
    }
}
