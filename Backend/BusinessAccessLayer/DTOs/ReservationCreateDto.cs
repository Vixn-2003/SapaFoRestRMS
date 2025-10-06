using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class ReservationCreateDto
    {
        public string CustomerName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public DateTime ReservationDate { get; set; }
        public DateTime ReservationTime { get; set; }
        public int NumberOfGuests { get; set; }
        public string? Notes { get; set; }
    }

}
