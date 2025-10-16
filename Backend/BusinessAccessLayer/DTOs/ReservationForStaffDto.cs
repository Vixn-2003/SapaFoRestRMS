using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class ReservationForStaffDto
    {
        public int ReservationId { get; set; }
        public string CustomerName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public DateTime ReservationDate { get; set; }
        public string TimeSlot { get; set; } = null!;
        public DateTime ReservationTime { get; set; }
        public int NumberOfGuests { get; set; }
        public string? Notes { get; set; }
        public bool DepositPaid { get; set; }
        public decimal? DepositAmount { get; set; }
        public List<TableDto> Tables { get; set; } = new List<TableDto>();
    }
}
