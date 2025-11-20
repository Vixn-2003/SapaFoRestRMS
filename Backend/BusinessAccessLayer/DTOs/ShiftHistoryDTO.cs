using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class ShiftHistoryDTO
    {
        public int HistoryId { get; set; }
        public int ShiftId { get; set; }
        public string ShiftName { get; set; } = null!;
        public int ActionBy { get; set; }
        public string Action { get; set; } = null!;
        public DateTime ActionAt { get; set; }
        public string? Detail { get; set; }
    }
}
