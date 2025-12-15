using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class ReservationCreateDto
    {
        [Required(ErrorMessage = "Tên khách hàng là bắt buộc.")]
        public string CustomerName { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "Ngày đặt bàn là bắt buộc.")]
        public DateTime ReservationDate { get; set; }

        [Required(ErrorMessage = "Giờ đặt bàn là bắt buộc.")]
        public DateTime ReservationTime { get; set; }

        [Range(1, 50, ErrorMessage = "Số lượng khách phải ít nhất 1 người.")]
        public int NumberOfGuests { get; set; }

        public string? Notes { get; set; }

        [Required(ErrorMessage = "OTP là bắt buộc.")]
        public string? OtpCode { get; set; }

        /// <summary>
        /// MOMO hoặc PAYOS (default PAYOS)
        /// </summary>
        public string PaymentMethod { get; set; } = "PAYOS";
    }

}
