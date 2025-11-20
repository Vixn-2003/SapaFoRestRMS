using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class WeeklyRecurringShiftDTO
    {
        public int RecurringId { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; } = null!;
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = null!;
        public string DaysOfWeek { get; set; } = null!;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
    }

    public class WeeklyRecurringShiftCreateDTO
    {
        public int StaffId { get; set; }
        public int TemplateId { get; set; }
        public string DaysOfWeek { get; set; } = null!; // "Mon,Wed,Fri"
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
    }
}
