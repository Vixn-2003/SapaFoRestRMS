using System;
using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho thông tin đơn hàng
/// </summary>
public class OrderDto
{
    public int OrderId { get; set; }

    public string? OrderCode { get; set; }

    public int? ReservationId { get; set; }

    public int? CustomerId { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerPhone { get; set; }

    public string? CustomerEmail { get; set; }

    public string OrderType { get; set; } = null!;

    public decimal? TotalAmount { get; set; }

    public decimal? Subtotal { get; set; }

    public decimal? VatAmount { get; set; }

    public decimal? ServiceFee { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal? DepositAmount { get; set; }

    public bool? DepositPaid { get; set; }

    /// <summary>
    /// Số tiền cần trả lại cho khách khi tiền cọc lớn hơn tổng tiền thanh toán
    /// </summary>
    public decimal? DepositRefundAmount { get; set; }

    /// <summary>
    /// Số tiền khách đưa khi thanh toán tiền mặt
    /// </summary>
    public decimal? AmountReceived { get; set; }

    /// <summary>
    /// Tiền thối lại cho khách khi thanh toán tiền mặt (khi AmountReceived > TotalAmount)
    /// </summary>
    public decimal? ChangeAmount { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? TableNumber { get; set; }

    public List<string> TableNumbers { get; set; } = new();

    public string? StaffName { get; set; }
    public string? WaiterName { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }

    public List<OrderItemDto> OrderItems { get; set; } = new List<OrderItemDto>();

    public List<TransactionDto> Transactions { get; set; } = new();
}

