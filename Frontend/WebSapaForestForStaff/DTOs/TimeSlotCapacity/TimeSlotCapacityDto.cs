namespace WebSapaForestForStaff.DTOs.TimeSlotCapacity
{
    public class TimeSlotCapacityDto
    {
        public string TimeSlot { get; set; } = null!;
        public int TotalCapacity { get; set; }
        public int ReservedGuests { get; set; }
        public int RemainingCapacity { get; set; }
    }

    public class DayCapacitySummaryDto
    {
        public DateTime Date { get; set; }

        public int TotalRestaurantCapacity { get; set; }
        public int TotalReservedGuestsInDay { get; set; }
        public int RemainingCapacityForDay { get; set; }

        public List<TimeSlotCapacityDto> TimeSlots { get; set; } = new();
    }
}
