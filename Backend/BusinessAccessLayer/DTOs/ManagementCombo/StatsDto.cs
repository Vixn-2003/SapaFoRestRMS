using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.ManagementCombo
{
    public class StatsDto
    {
        // Nhãn thời gian để hiển thị (VD: "05/12", "Tuần 1", "Tháng 10")
        public string Label { get; set; } = string.Empty;

        // Giá trị số liệu (Số lượng Combo bán được)
        public int Value { get; set; }

        // (Tùy chọn mở rộng) Nếu bạn muốn gửi kèm Doanh thu để vẽ 2 đường biểu đồ
        // public decimal TotalRevenue { get; set; } 
    }
}
