namespace WebSapaForestForStaff.DTOs
{
    public class PurchaseOrderDTO
    {
        public string PurchaseOrderId { get; set; } = null!;
        public int SupplierId { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? TimeConfirm { get; set; }
        public string? Status { get; set; }
        public int? IdCreator { get; set; }
        public int? IdConfirm { get; set; }
        public string? UrlImg { get; set; }

        public List<PurchaseOrderDetailDTO> PurchaseOrderDetails { get; set; }
        public SupplierDTO Supplier { get; set; }
    }
}
