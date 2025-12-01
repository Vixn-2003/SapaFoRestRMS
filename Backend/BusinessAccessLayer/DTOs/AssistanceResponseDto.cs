using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class AssistanceResponseDto
    {
        public int RequestId { get; set; }
        public string TableName { get; set; } // "Bàn 05"
        public string AreaName { get; set; }  // "Tầng 1"

        public int Floor { get; set; }
        public string Note { get; set; }
        public DateTime RequestTime { get; set; }
        public string TimeAgo { get; set; }   // "Vừa xong", "5 phút trước"
    }
}
