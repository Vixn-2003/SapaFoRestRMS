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

    public string OrderType { get; set; } = null!;

    public decimal? TotalAmount { get; set; }

    public decimal? Subtotal { get; set; }

    public decimal? VatAmount { get; set; }

    public decimal? ServiceFee { get; set; }

    public decimal? DiscountAmount { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? TableNumber { get; set; }

    public string? StaffName { get; set; }

    public List<OrderItemDto> OrderItems { get; set; } = new List<OrderItemDto>();
}

