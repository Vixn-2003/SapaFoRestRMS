using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Shift
{
    public class CreateShiftDTO
    {
        public DateTime Date { get; set; }
        public int TemplateId { get; set; }
        public int DepartmentId { get; set; }
    }

    public class UpdateShiftDTO
    {
        public DateTime Date { get; set; }
        public int TemplateId { get; set; }
        public int DepartmentId { get; set; }
    }

    public class ShiftViewDTO
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = null!;
        public string Code { get; set; } = null!;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int RequiredEmployees { get; set; }
    }
}
