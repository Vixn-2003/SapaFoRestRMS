namespace WebSapaForestForStaff.DTOs.OrderGuest.ListOrder
{
    public class PaginationInfo
    {
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; } // Tên trong header là "PageNumber"
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
}
