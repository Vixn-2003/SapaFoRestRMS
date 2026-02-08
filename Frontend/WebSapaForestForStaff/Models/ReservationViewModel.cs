using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.Models
{
    public class ReservationViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Tên khách hàng là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên khách hàng không được vượt quá 100 ký tự.")]
        [Display(Name = "Tên khách hàng")]
        public string CustomerName { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [RegularExpression(@"^\d{9,11}$", ErrorMessage = "Số điện thoại phải gồm từ 9 đến 11 chữ số.")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "Ngày đặt là bắt buộc.")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày đặt")]
        public DateTime? ReservationDate { get; set; }

        [Required(ErrorMessage = "Thời gian là bắt buộc.")]
        [DataType(DataType.Time)]
        [Display(Name = "Giờ đặt")]
        public DateTime? ReservationTime { get; set; }

        [Required(ErrorMessage = "Số lượng khách là bắt buộc")]
        [Range(1, 100, ErrorMessage = "Số lượng khách phải từ 1 trở lên")]
        [Display(Name = "Số lượng khách")]
        public int NumberOfGuests { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ReservationDate.HasValue)
            {
                DateTime today = DateTime.Today;
                if (ReservationDate.Value.Date < today)
                {
                    yield return new ValidationResult(
                        "Ngày đặt phải lớn hơn hoặc bằng ngày hiện tại.",
                        new[] { nameof(ReservationDate) });
                }
            }

            if (ReservationDate.HasValue && ReservationTime.HasValue)
            {
                // Gộp ngày và giờ để so sánh chính xác
                DateTime reservationDateTime = ReservationDate.Value.Date
                                               .AddHours(ReservationTime.Value.Hour)
                                               .AddMinutes(ReservationTime.Value.Minute);
                DateTime now = DateTime.Now;

                if (reservationDateTime < now)
                {
                    yield return new ValidationResult(
                        "Giờ đặt phải lớn hơn hoặc bằng thời điểm hiện tại.",
                        new[] { nameof(ReservationTime) });
                }
            }
        }

        }
}
