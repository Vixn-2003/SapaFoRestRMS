using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.OrderGuest.ListOrder
{
    public class ReservationListDto
    {
        public int ReservationId { get; set; } // Thay đổi
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string Areas { get; set; }
        public string Tables { get; set; }
        public TimeSpan ReservationTime { get; set; } // Thay đổi: time(7) -> TimeSpan
        public DateTime? ArrivalAt { get; set; } // datetime2(7) -> DateTime?
        public string TimeSlot { get; set; }
        public string Status { get; set; }
    }

}
