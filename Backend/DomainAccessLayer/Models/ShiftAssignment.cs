using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainAccessLayer.Models
{
    public class ShiftAssignment
    {
        public int Id { get; set; }
        public int ShiftId { get; set; }
        public int StaffId { get; set; }

        public virtual Shift Shift { get; set; } = null!;
        public virtual Staff Staff { get; set; } = null!;
    }

}
