using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WebSapaForestForStaff.DTOs.Payment
{
    /// <summary>
    /// Thông tin chi tiết của một đơn hàng
    /// </summary>
    public class OrderDetailDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public List<string>? TableNumbers { get; set; }
        public string? TableNumber { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? StaffName { get; set; }
        public string? WaiterName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? PaymentMethod { get; set; }
        [JsonPropertyName("orderItems")]
        public List<OrderItemDto> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal VatAmount { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal? DepositAmount { get; set; }
        public bool? DepositPaid { get; set; }
        public decimal TotalAmount { get; set; }

        // Legacy alias
        public int Id
        {
            get => OrderId;
            set => OrderId = value;
        }

        public string? Table
        {
            get => !string.IsNullOrEmpty(TableNumber)
                ? TableNumber
                : (TableNumbers != null && TableNumbers.Count > 0 ? string.Join(", ", TableNumbers) : null);
            set => TableNumber = value;
        }

        public decimal Total
        {
            get => TotalAmount != 0 ? TotalAmount : Subtotal;
            set => TotalAmount = value;
        }

        // 🔖 Thông tin ưu đãi hiện tại (nếu backend có trả về thêm)
        public string? AppliedVoucherCode { get; set; }
        public int? AppliedPromotionId { get; set; }
    }
}

