
namespace WebSapaForestForStaff.DTOs.OrderGuest.ListOrder
{
    public class ReservationListViewModel
    {
        public List<ReservationListDto> Reservations { get; set; } = new List<ReservationListDto>();
        public PaginationInfo Pagination { get; set; } = new PaginationInfo();
    }
}
