using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.DTOs
{
    public class TimeSlotCapacityDto
    {
        public string TimeSlot { get; set; } = null!;
        public int TotalCapacity { get; set; }          // Tổng sức chứa nhà hàng
        public int ReservedGuests { get; set; }         // Số khách đã đặt trong ca đó
        public int RemainingCapacity { get; set; }      // Sức chứa còn lại cho ca
    }

    public class DayCapacitySummaryDto
    {
        public DateTime Date { get; set; }

        public int TotalRestaurantCapacity { get; set; }      // Tổng sức chứa nhà hàng
        public int TotalReservedGuestsInDay { get; set; }     // Tổng số khách đã đặt cả ngày
        public int RemainingCapacityForDay { get; set; }      // Sức chứa còn lại trong ngày

        public List<TimeSlotCapacityDto> TimeSlots { get; set; } = new();
    }
}
