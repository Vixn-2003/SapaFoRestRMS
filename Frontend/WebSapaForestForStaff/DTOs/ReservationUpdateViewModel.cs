using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.DTOs
{
    public class ReservationUpdateViewModel
    {
        [Required]
        public int ReservationId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày đặt bàn.")]
        public DateTime ReservationDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giờ đến.")]
        public DateTime ReservationTime { get; set; }

        [Range(1, 50, ErrorMessage = "Số khách phải từ 1 đến 50.")]
        public int NumberOfGuests { get; set; }

        public string? Notes { get; set; }
    }
}
