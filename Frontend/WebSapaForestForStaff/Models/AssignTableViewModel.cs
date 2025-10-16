using static System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy;

namespace WebSapaForestForStaff.Models
{
    public class AssignTableViewModel
    {
        public int ReservationId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public DateTime ReservationDate { get; set; }
        public string TimeSlot { get; set; } = string.Empty;
        public int NumberOfGuests { get; set; }

        public bool RequireDeposit { get; set; }
        public decimal? DepositAmount { get; set; }

        public List<TableInfo> Tables { get; set; } = new();
    }
}
