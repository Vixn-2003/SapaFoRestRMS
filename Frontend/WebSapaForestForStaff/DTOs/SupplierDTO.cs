namespace WebSapaForestForStaff.DTOs
{
    public class SupplierDTO
    {
        public int SupplierId { get; set; }

        public string Name { get; set; } = null!;

        public string? ContactInfo { get; set; }

        public string? Address { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string CodeSupplier { get; set; } = string.Empty!;
    }
}
