using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainAccessLayer.Models
{
    public class DayCalendar
    {
        public int Id { get; set; }
        public DateTime WorkDate { get; set; }
        public int DayTypeId { get; set; }

        public virtual DayType DayType { get; set; } = null!;
    }

}
