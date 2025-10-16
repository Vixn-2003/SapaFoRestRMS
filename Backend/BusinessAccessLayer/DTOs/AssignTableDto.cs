using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class AssignTableDto
    {
        public int ReservationId { get; set; }
        public List<int> TableIds { get; set; } = new();
        public bool RequireDeposit { get; set; }
        public decimal? DepositAmount { get; set; }
        public int StaffId { get; set; }
        public bool ConfirmBooking { get; set; } = false;
    }
}
