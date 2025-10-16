using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebSapaFoRestForCustomer.Models
{
    public class ReservationViewModel : IValidatableObject
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

        // Kiểm tra logic tổng hợp ngày & giờ đặt
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ReservationDate.HasValue)
            {
                var today = DateTime.Today;
                if (ReservationDate.Value.Date < today)
                {
                    yield return new ValidationResult(
                        "Ngày đặt phải lớn hơn hoặc bằng ngày hiện tại.",
                        new[] { nameof(ReservationDate) });
                }

                // Nếu ngày là hôm nay -> kiểm tra giờ
                if (ReservationDate.Value.Date == today && ReservationTime.HasValue)
                {
                    var now = DateTime.Now;
                    var selectedDateTime = new DateTime(
                        ReservationDate.Value.Year,
                        ReservationDate.Value.Month,
                        ReservationDate.Value.Day,
                        ReservationTime.Value.Hour,
                        ReservationTime.Value.Minute,
                        0);

                    if (selectedDateTime < now)
                    {
                        yield return new ValidationResult(
                            "Giờ đặt phải lớn hơn hoặc bằng thời điểm hiện tại.",
                            new[] { nameof(ReservationTime) });
                    }
                }
            }
        }
    }
}
