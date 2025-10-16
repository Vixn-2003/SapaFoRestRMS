namespace WebSapaForestForStaff.DTOs.TableManage
{
    public class TableUpdateDto
    {
        public string TableNumber { get; set; } = null!;
        public int Capacity { get; set; }
        public string Status { get; set; } = "Available";
        public int AreaId { get; set; }
    }
}
