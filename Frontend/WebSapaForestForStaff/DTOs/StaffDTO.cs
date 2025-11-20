namespace WebSapaForestForStaff.DTOs
{
    public class StaffDTO
    {
        public int StaffId { get; set; }
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public decimal SalaryBase { get; set; }
        public int Status { get; set; }
    }
}
