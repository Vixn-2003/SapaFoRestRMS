using System.Text.Json.Serialization;
using System.Collections.Generic;
using System;

namespace WebSapaForestForStaff.DTOs.Payment
{
    /// <summary>
    /// Thông tin đơn hàng dùng cho màn OrderSelection
    /// </summary>
    public class OrderDto
    {
        public int OrderId { get; set; }
        public string? OrderCode { get; set; }
        public int? ReservationId { get; set; }
        public List<string>? TableNumbers { get; set; }
        public string? TableNumber { get; set; }
        public string? CustomerName { get; set; }
        public string? StaffName
        {
            get => _staffName;
            set
            {
                _staffName = value;
                if (string.IsNullOrEmpty(_waiterName))
                {
                    _waiterName = value;
                }
            }
        }

        public string? WaiterName
        {
            get => _waiterName ?? _staffName;
            set => _waiterName = value;
        }
        public decimal Subtotal { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? PaymentMethod { get; set; }

        // Legacy aliases for compatibility
        [JsonIgnore]
        public int Id
        {
            get => OrderId;
            set => OrderId = value;
        }

        [JsonIgnore]
        public string? Table
        {
            get => !string.IsNullOrEmpty(TableNumber)
                ? TableNumber
                : (TableNumbers != null && TableNumbers.Count > 0 ? string.Join(", ", TableNumbers) : null);
            set => TableNumber = value;
        }

        [JsonIgnore]
        public decimal Total
        {
            get => TotalAmount != 0 ? TotalAmount : Subtotal;
            set => TotalAmount = value;
        }

        private string? _staffName;
        private string? _waiterName;
    }
}

