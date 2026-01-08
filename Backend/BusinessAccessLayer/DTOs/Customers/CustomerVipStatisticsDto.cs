namespace BusinessAccessLayer.DTOs.Customers
{
    public class CustomerVipStatisticsDto
    {
        public int CustomerId { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public decimal TotalSpend { get; set; }
        public int TotalBills { get; set; }
        public int TotalGuests { get; set; }
        public decimal AverageSpendPerPerson { get; set; }
        public decimal AverageSpendPerBill { get; set; }
        public int NumberOfReservations { get; set; }
        public bool ComputedVip { get; set; }
        public bool IsVip { get; set; }
        public bool IsManualOverride { get; set; }
    }
}

