using System.Collections.Generic;

namespace BusinessAccessLayer.Common.Pagination
{
    public class PagedRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string? Search { get; set; }
        public string? SortBy { get; set; }
        public string SortDirection { get; set; } = "asc"; // asc | desc

        // Simple key=value filters, e.g. { "Status": "1" }
        public Dictionary<string, string>? Filters { get; set; }
    }
}


