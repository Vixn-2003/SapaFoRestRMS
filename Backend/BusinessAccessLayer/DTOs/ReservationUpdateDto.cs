using System;

namespace BusinessAccessLayer.DTOs
{
    public class ReservationUpdateDto
    {
        public DateTime ReservationDate { get; set; }
        public DateTime ReservationTime { get; set; }
        public int NumberOfGuests { get; set; }
        public string? Notes { get; set; }
    }
}
