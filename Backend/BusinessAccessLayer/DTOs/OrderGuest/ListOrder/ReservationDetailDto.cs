using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.OrderGuest.ListOrder
{
    public class ReservationDetailDto
    {
        public int ReservationId { get; set; } // Thay đổi
        public string Status { get; set; }
        public string Notes { get; set; }

        // Thông tin khách hàng
        public int? CustomerId { get; set; } // Thay đổi
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }

        // Thông tin đặt chỗ
        public DateTime ReservationDate { get; set; } // ERD là 'date' -> DateTime
        public string TimeSlot { get; set; }
        public TimeSpan ReservationTime { get; set; } // Thay đổi: time(7) -> TimeSpan
        public int NumberOfGuests { get; set; }
        public decimal DepositAmount { get; set; }
        public bool DepositPaid { get; set; }

        // Thông tin bàn và vị trí
        public List<TableDetailDto> AssignedTables { get; set; } = new List<TableDetailDto>();
    }
}
