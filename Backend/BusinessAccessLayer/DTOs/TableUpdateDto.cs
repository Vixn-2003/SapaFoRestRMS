using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class TableUpdateDto
    {
        public string TableNumber { get; set; } = null!;
        public int Capacity { get; set; }
        public string Status { get; set; } = "Available";
        public int AreaId { get; set; }
    }
}
