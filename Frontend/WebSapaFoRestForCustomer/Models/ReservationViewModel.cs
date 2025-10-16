using System;
using System.ComponentModel.DataAnnotations;

namespace WebSapaFoRestForCustomer.Models
{
    public class ReservationViewModel
    {
        [Required(ErrorMessage = "Tên khách hàng là bắt buộc")]
        [Display(Name = "Tên khách hàng")]
        public string CustomerName { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "Ngày đặt là bắt buộc")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày đặt")]
        public DateTime? ReservationDate { get; set; }

        [Required(ErrorMessage = "Thời gian là bắt buộc")]
        [DataType(DataType.Time)]
        [Display(Name = "Thời gian")]
        public DateTime? ReservationTime { get; set; }

        [Required(ErrorMessage = "Số lượng khách là bắt buộc")]
        [Range(1, 100, ErrorMessage = "Số lượng khách phải từ 1 trở lên")]
        [Display(Name = "Số lượng khách")]
        public int NumberOfGuests { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Display(Name = "Mã OTP")]
        public string? OtpCode { get; set; }
    }
}
