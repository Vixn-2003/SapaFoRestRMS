namespace WebSapaForestForStaff.DTOs.Payment
{
    /// <summary>
    /// Request DTO cho việc khách hàng xác nhận lại đơn hàng (chỉnh số lượng, bỏ món)
    /// </summary>
    public class ConfirmOrderRequest
    {
        public int OrderId { get; set; }
        public List<ConfirmedItemDto> Items { get; set; } = new();
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO cho món ăn đã được khách xác nhận
    /// </summary>
    public class ConfirmedItemDto
    {
        public int OrderDetailId { get; set; }
        public int QuantityUsed { get; set; }
        public bool IsRemoved { get; set; }
    }
}

