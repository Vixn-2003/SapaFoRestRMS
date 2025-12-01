using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.ShiftTemplateDTOs
{
    public class ShiftTemplateCreateDTO
    {
        public int DayTypeId { get; set; }
        public int DepartmentId { get; set; }
        public string Code { get; set; } = null!;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int RequiredEmployees { get; set; }
    }

    public class ShiftTemplateUpdateDTO
    {
        public int DayTypeId { get; set; }
        public int DepartmentId { get; set; }
        public string Code { get; set; } = null!;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int RequiredEmployees { get; set; }
    }

    public class ShiftTemplateResponseDTO
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int RequiredEmployees { get; set; }

        public int DayTypeId { get; set; }
        public string DayTypeName { get; set; } = null!;

        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = null!;
    }
}
