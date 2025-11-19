using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public class DashboardDataDto
    {
        // Danh sách bàn đã lọc
        public List<TableDashboardDto> Tables { get; set; } = new();      

        // Tổng số lượng bản ghi (để tính toán số trang)
        public int TotalCount { get; set; }
        // Dữ liệu cho bộ lọc
        public List<string> AreaNames { get; set; } = new();
        public List<int?> Floors { get; set; } = new();
    }
}
