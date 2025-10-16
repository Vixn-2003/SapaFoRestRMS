using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainAccessLayer.Models
{
    public class Area
    {
        public int AreaId { get; set; }

        public string AreaName { get; set; } = null!;

        public int Floor { get; set; }  

        public string? Description { get; set; }

       
        public virtual ICollection<Table> Tables { get; set; } = new List<Table>();
    }
}
