using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.OrderAssitance
{
    public class PagedResult<T>
    {
        // Danh sách dữ liệu của trang hiện tại
        public IEnumerable<T> Items { get; set; } = new List<T>();

        // Tổng số bản ghi trong database (để tính tổng số trang)
        public int TotalCount { get; set; }

        // Trang hiện tại (1, 2, 3...)
        public int Page { get; set; }

        // Số lượng bản ghi trên 1 trang
        public int PageSize { get; set; }

        // (Tùy chọn) Tính tổng số trang
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    }
}
