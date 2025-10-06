using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class TableDto
    {
        public int TableId { get; set; }
        public string TableNumber { get; set; } = null!;
        public int Capacity { get; set; }
        public string Status { get; set; }  // Available, Reserved
    }
}
