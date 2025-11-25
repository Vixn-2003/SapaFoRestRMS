using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class AuditInventoryRequestDTO
    {
        public string? AuditId { get; set; }
        public int BatchId { get; set; }  
        public string PurchaseOrderId { get; set; }         // Mã lô (PO)
        public string IngredientCode { get; set; }
        public string IngredientName { get; set; }
        public string Unit { get; set; }
        public decimal OriginalQuantity { get; set; }       // Số lượng gốc
        public DateOnly? ExpiryDate { get; set; }           // Hạn sử dụng

        public int CreatorId { get; set; }                  // ID người tạo đơn
        public DateTime CreatedAt { get; set; }             // Thời gian tạo đơn

        public string Reason { get; set; }                  // Lý do kiểm kê
        public decimal AdjustmentQuantity { get; set; }     // Số lượng chỉnh sửa
        public bool IsAddition { get; set; }                // true = cộng, false = trừ

        public string IngredientStatus { get; set; }        // Trạng thái nguyên liệu
        public string AuditStatus { get; set; }             // Trạng thái đơn (processing/completed)

        public IFormFile? ImageFile { get; set; }           // Hình ảnh

        // Thông tin người tạo đơn (để lưu vào DB)
        public string CreatorName { get; set; }
        public string CreatorPosition { get; set; }
        public string CreatorPhone { get; set; }
    }
}
