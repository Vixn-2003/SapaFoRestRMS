namespace WebSapaForestForStaff.DTOs
{
    public class BookedTableResultWithTime
    {
        public List<BookedTableDetailDto> BookedTables { get; set; }
    }

    public class BookedTableDetailDto
    {
        public int TableId { get; set; }
        public DateTime ReservationTime { get; set; }
    }
}
