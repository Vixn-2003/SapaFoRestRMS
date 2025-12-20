using System;
using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho thông tin thanh toán theo Reservation (tổng hợp tất cả Orders)
/// </summary>
public class ReservationPaymentDto
{
    public int ReservationId { get; set; }

    public int? CustomerId { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerPhone { get; set; }

    public string? CustomerEmail { get; set; }

    public List<string> TableNumbers { get; set; } = new();

    public string? TableNumber { get; set; }

    public string? StaffName { get; set; }

    public string? WaiterName { get; set; }

    /// <summary>
    /// Danh sách tất cả Orders thuộc Reservation này
    /// </summary>
    public List<OrderDto> Orders { get; set; } = new();

    /// <summary>
    /// Tổng tiền tạm tính (tổng tất cả OrderDetails từ tất cả Orders)
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// VAT (10%)
    /// </summary>
    public decimal VatAmount { get; set; }

    /// <summary>
    /// Phí dịch vụ (5%)
    /// </summary>
    public decimal ServiceFee { get; set; }

    /// <summary>
    /// Tổng giảm giá (từ voucher/discount)
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Tổng tiền thanh toán cuối cùng
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Tiền cọc (nếu có)
    /// </summary>
    public decimal? DepositAmount { get; set; }

    /// <summary>
    /// Đã thanh toán cọc chưa
    /// </summary>
    public bool? DepositPaid { get; set; }

    /// <summary>
    /// Số tiền cần trả lại cho khách khi tiền cọc lớn hơn tổng tiền thanh toán
    /// </summary>
    public decimal? DepositRefundAmount { get; set; }

    /// <summary>
    /// Trạng thái Reservation
    /// </summary>
    public string? ReservationStatus { get; set; }

    /// <summary>
    /// Tất cả OrderItems từ tất cả Orders (phẳng hóa để hiển thị)
    /// </summary>
    public List<OrderItemDto> AllOrderItems { get; set; } = new();

    /// <summary>
    /// Danh sách tất cả Transactions của Reservation (từ tất cả Orders)
    /// </summary>
    public List<TransactionDto> Transactions { get; set; } = new();

    /// <summary>
    /// Số lượng Orders trong Reservation
    /// </summary>
    public int OrderCount => Orders.Count;

    /// <summary>
    /// Ngày tạo Reservation
    /// </summary>
    public DateTime? CreatedAt { get; set; }
}

