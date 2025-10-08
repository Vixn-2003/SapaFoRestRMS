namespace WebSapaForestForStaff.Models
{
    public class ReservationListViewModel
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<ReservationStaffViewModel> Data { get; set; } = new();
    }
}
