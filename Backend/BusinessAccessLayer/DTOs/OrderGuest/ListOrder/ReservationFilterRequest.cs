using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.OrderGuest.ListOrder
{
    public class ReservationFilterRequest
    {
        public string? Search { get; set; }
        public DateTime? Date { get; set; }
        public int? AreaId { get; set; }
        public string? TimeSlot { get; set; }
        public int PageIndex { get; set; } = 0;
        public int PageSize { get; set; } = 10;
    }

}
