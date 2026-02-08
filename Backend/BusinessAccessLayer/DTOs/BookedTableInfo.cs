using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class BookedTableInfo
    {
        public int TableId { get; set; }
        public DateTime ReservationTime { get; set; }
    }

}
