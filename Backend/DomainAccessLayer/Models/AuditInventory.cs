using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainAccessLayer.Models
{
    public class AuditInventory
    {
        [Key]
        [MaxLength(50)]  
        public string AuditId { get; set; } = null!;

        [Required]
        public int BatchId { get; set; }

        [Required]
        [MaxLength(50)]
        public string PurchaseOrderId { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string IngredientCode { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string ingredientName { get; set; } = null!;
        
        [Required]
        [MaxLength(50)]
        public string unit { get; set; } = null!;



        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal OriginalQuantity { get; set; }

        public DateOnly? ExpiryDate { get; set; }

        // Thông tin người tạo đơn
        [Required]
        public int CreatorId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        [MaxLength(100)]
        public string CreatorName { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string CreatorPosition { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string CreatorPhone { get; set; } = null!;

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal AdjustmentQuantity { get; set; }

        [Required]
        public bool IsAddition { get; set; }

        [MaxLength(50)]
        public string? IngredientStatus { get; set; }

        [Required]
        [MaxLength(20)]
        public string AuditStatus { get; set; }

        [MaxLength(500)]
        public string? ImagePath { get; set; }

        // Thông tin người xác nhận (nullable - sẽ được cập nhật sau)
        public int? ConfirmerId { get; set; }

        public DateTime? ConfirmedAt { get; set; }

        [MaxLength(100)]
        public string? ConfirmerName { get; set; }

        [MaxLength(100)]
        public string? ConfirmerPosition { get; set; }

        [MaxLength(20)]
        public string? ConfirmerPhone { get; set; }

        // Navigation properties
        [ForeignKey("CreatorId")]
        public virtual User? Creator { get; set; }

        [ForeignKey("ConfirmerId")]
        public virtual User? Confirmer { get; set; }
    }
}
