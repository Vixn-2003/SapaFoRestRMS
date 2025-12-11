namespace BusinessAccessLayer.DTOs.Waiter
{
    /// <summary>
    /// Request để cập nhật số lượng cho món có BillingType = 1 (ConsumptionBased)
    /// Cho phép tăng/giảm số lượng kể cả sau khi xác nhận
    /// </summary>
    public class UpdateQuantityDto
    {
        public int OrderDetailId { get; set; }
        /// <summary>
        /// OrderComboItemId - null nếu là món lẻ, có giá trị nếu là món trong combo
        /// </summary>
        public int? OrderComboItemId { get; set; }
        public int Quantity { get; set; } // Số lượng mới
    }

    /// <summary>
    /// Response sau khi cập nhật số lượng
    /// </summary>
    public class UpdateQuantityResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

