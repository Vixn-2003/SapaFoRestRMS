using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.OrderGuest.ListOrder
{
    public class TableDetailDto
    {
        public int TableId { get; set; } 
        public string TableNumber { get; set; }
        public int Capacity { get; set; }
        public string AreaName { get; set; }
        public int Floor { get; set; }
        public string Position => $"{AreaName} - {TableNumber}";
    }
}
