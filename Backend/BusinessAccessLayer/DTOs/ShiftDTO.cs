using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class ShiftDTO
    {
        public int ShiftId { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; } = null!;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string ShiftType { get; set; } = null!;
        public string? Note { get; set; }
        public int Status { get; set; }
    }

    public class ShiftCreateDTO
    {
        public int StaffId { get; set; }
        public int DepartmentId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string ShiftType { get; set; } = null!;
        public string? Note { get; set; }
        public int? TemplateId { get; set; }
        public int? RecurringId { get; set; }
    }

    public class ShiftUpdateDTO
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? ShiftType { get; set; }
        public string? Note { get; set; }
        public int? StaffId { get; set; }
        public int? DepartmentId { get; set; }
    }
    public class ShiftCreateWithRepeatDTO
    {
        public List<int> StaffIds { get; set; }
        public int TemplateId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string RepeatType { get; set; } // none, daily, weekly
        public int? RepeatDays { get; set; }
        public int? RepeatWeeks { get; set; }
    }

}
