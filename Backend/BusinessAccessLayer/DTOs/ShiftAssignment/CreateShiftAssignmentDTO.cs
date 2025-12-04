using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.ShiftAssignment
{
    public class CreateShiftAssignmentDTO
    {
        public int ShiftId { get; set; }
        public int StaffId { get; set; }
    }

    public class UpdateShiftAssignmentDTO
    {
        public int ShiftId { get; set; }
        public int StaffId { get; set; }
    }

    public class ShiftAssignmentViewDTO
    {
        public int Id { get; set; }
        public int ShiftId { get; set; }
        public string ShiftCode { get; set; } = null!;
        public DateTime ShiftDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public int StaffId { get; set; }
        public string StaffName { get; set; } = null!;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = null!;
    }

}
