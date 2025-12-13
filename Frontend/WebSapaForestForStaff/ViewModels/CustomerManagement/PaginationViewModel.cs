using System;

namespace WebSapaForestForStaff.ViewModels.CustomerManagement
{
    /// <summary>
    /// ViewModel for pagination information
    /// </summary>
    public class PaginationViewModel
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

        public int StartRecord => (PageNumber - 1) * PageSize + 1;
        public int EndRecord => Math.Min(PageNumber * PageSize, TotalRecords);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}

