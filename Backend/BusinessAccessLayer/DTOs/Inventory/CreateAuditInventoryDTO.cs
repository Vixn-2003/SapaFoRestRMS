using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class CreateAuditInventoryDTO
    {
        public string PurchaseOrderId { get; set; } = null!;
        public string IngredientCode { get; set; } = null!;
        public decimal OriginalQuantity { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public int CreatorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatorName { get; set; } = null!;
        public string CreatorPosition { get; set; } = null!;
        public string CreatorPhone { get; set; } = null!;
        public string Reason { get; set; } = null!;
        public decimal AdjustmentQuantity { get; set; }
        public bool IsAddition { get; set; }
        public string? IngredientStatus { get; set; }
        public IFormFile? ImageFile { get; set; }
    }

    // DTO cho Confirm
    public class ConfirmAuditInventoryDTO
    {
        public string AuditId { get; set; }
        public string AuditStatus { get; set; }
        public int ConfirmerId { get; set; }
        public string ConfirmerName { get; set; }
        public string ConfirmerPhone { get; set; }
        public string ConfirmerPosition { get; set; }
        public DateTime ConfirmedAt { get; set; }
    }

    // DTO cho Response
    public class AuditInventoryResponseDTO
    {
        public string AuditId { get; set; }
        public string PurchaseOrderId { get; set; } = null!;
        public string IngredientCode { get; set; } = null!;
        public string ingredientName { get; set; } = null!;
        public decimal OriginalQuantity { get; set; }
        public decimal AdjustmentQuantity { get; set; }
        public bool IsAddition { get; set; }
        public decimal NewQuantity { get; set; }
        public string unit { get; set; } = null!;
        public string Reason { get; set; } = null!;
        public string AuditStatus { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string CreatorName { get; set; } = null!;
        public string CreatorPosition { get; set; } = null!;
        public string CreatorPhone { get; set; } = null!;
        public DateTime? ConfirmedAt { get; set; }
        public int? ConfirmerId { get; set; }
        public string? ConfirmerName { get; set; }
        public string? ConfirmerPosition { get; set; }
        public string? ConfirmerPhone { get; set; }
        public string? ImagePath { get; set; }
        public string? IngredientStatus { get; set; }
        public DateOnly? ExpiryDate { get; set; }
    }
}
