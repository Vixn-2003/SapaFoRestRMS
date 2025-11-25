using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Common
{
    // Trong DataAccessLayer/Common/PagedList.cs
    public class PagedList<T>
    {
        public List<T> Items { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public bool HasNextPage => PageNumber * PageSize < TotalCount;
        public bool HasPreviousPage => PageNumber > 1;

        public PagedList(List<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}
