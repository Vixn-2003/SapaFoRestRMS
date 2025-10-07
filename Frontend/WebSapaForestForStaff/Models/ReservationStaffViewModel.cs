namespace WebSapaForestForStaff.Models
{
    public class ReservationStaffViewModel
    {
        public int ReservationId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public DateTime ReservationDate { get; set; }
        public string TimeSlot { get; set; } = string.Empty;
        public int NumberOfGuests { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<int> TableIds { get; set; } = new();
        public bool RequireDeposit { get; set; }
        public decimal? DepositAmount { get; set; }
    }
}
