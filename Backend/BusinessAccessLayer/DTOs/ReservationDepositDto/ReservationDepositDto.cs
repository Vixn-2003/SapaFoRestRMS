using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.ReservationDepositDto
{
    public class ReservationDepositDto
    {
        public int ReservationId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public string DepositCode { get; set; } = null!;
        public string? Notes { get; set; }
        public IFormFile? ReceiptImage { get; set; } // Ảnh biên lai upload
    }
}
