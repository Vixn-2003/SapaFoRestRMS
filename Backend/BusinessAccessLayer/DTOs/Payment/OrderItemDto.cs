namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho chi tiết món ăn trong đơn hàng
/// </summary>
public class OrderItemDto
{
    public int OrderDetailId { get; set; }

    public int MenuItemId { get; set; }

    public string MenuItemName { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public string? Status { get; set; }

    public string? Notes { get; set; }
}

