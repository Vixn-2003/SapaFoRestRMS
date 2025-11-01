using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class PayrollDTO
    {
        public int PayrollId { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; } = null!;
        public string MonthYear { get; set; } = null!;
        public decimal BaseSalary { get; set; }
        public int? TotalWorkDays { get; set; }
        public decimal? TotalBonus { get; set; }
        public decimal? TotalPenalty { get; set; }
        public decimal? NetSalary { get; set; }
        public string? Status { get; set; }
    }
}
