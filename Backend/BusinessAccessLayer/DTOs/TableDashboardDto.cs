using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class TableDashboardDto
    {
        public int TableId { get; set; }
        public string TableNumber { get; set; }
        public string AreaName { get; set; }
        public int? Floor { get; set; }
        public int Capacity { get; set; }

        // "Trống" hoặc "Hoạt động"
        public string Status { get; set; }

        // Lấy từ Reservation
        public int GuestCount { get; set; }

        // (MỚI) Thêm trường này để đếm giờ
        public DateTime? GuestSeatedTime { get; set; }

        public DateTime? ReservationTime { get; set; }

        //  THÊM 2 TRƯỜNG NÀY 
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal GrandTotal { get; set; }

        public int? reservationId { get; set; }
    }
}
