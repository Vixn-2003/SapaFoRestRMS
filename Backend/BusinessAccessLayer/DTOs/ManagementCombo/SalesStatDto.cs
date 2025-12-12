using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.ManagementCombo
{
    public class SalesStatDto
    {
        public string Label { get; set; } // Ví dụ: "Thứ 2", "05/12", "Tháng 1"
        public int TotalQuantity { get; set; } // Số lượng bán ra
        // public decimal TotalRevenue { get; set; } // Nếu cần thêm doanh thu tiền
    }
}
