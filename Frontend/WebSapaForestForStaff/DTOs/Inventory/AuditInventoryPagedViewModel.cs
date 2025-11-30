namespace WebSapaForestForStaff.DTOs.Inventory
{
    public class AuditInventoryPagedViewModel
    {
        public List<AuditInventoryDTO> Audits { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int ItemsPerPage { get; set; } = 10;
        public int TotalItems { get; set; }

        // Filter properties
        public string? SearchTerm { get; set; }
        public string? FilterStatus { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalItems / ItemsPerPage);
    }

    public class AuditInventoryDTO
    {
        public string AuditId { get; set; }
        public string PurchaseOrderId { get; set; } = null!;
        public string IngredientCode { get; set; } = null!;
        public string IngredientName { get; set; } = null!;
        public decimal OriginalQuantity { get; set; }
        public decimal AdjustmentQuantity { get; set; }
        public bool IsAddition { get; set; }
        public decimal NewQuantity { get; set; }
        public string Unit { get; set; } = null!;
        public string Reason { get; set; } = null!;
        public string AuditStatus { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string CreatorName { get; set; } = null!;
        public string CreatorPosition { get; set; } = null!;
        public string CreatorPhone { get; set; } = null!;
        public DateTime? ConfirmedAt { get; set; }
        public string? ConfirmerName { get; set; }
        public string? ConfirmerPosition { get; set; }
        public string? ConfirmerPhone { get; set; }
        public string? ImagePath { get; set; }
        public string? IngredientStatus { get; set; }
        public string? ExpiryDate { get; set; }
    }
}
