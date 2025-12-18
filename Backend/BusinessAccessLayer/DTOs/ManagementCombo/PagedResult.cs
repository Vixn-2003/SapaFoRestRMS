using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.ManagementCombo
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalRecords { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
        public PagedResult() { }

        public PagedResult(List<T> items, int count, int pageIndex, int pageSize)
        {
            Items = items;
            TotalRecords = count;
            PageIndex = pageIndex;
            PageSize = pageSize;
        }
    }
}
