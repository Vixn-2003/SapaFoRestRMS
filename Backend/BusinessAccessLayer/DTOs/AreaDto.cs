using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class AreaDto
    {
        public int AreaId { get; set; }
        public string AreaName { get; set; } = null!;
        public int Floor { get; set; }
        public string? Description { get; set; }
    }
}
