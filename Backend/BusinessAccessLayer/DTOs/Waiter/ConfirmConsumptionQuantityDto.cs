namespace BusinessAccessLayer.DTOs.Waiter
{
    /// <summary>
    /// Request để xác nhận số lượng đã lấy cho món có BillingType = 1 (ConsumptionBased)
    /// Không cần chờ bếp, phục vụ có thể tự chủ động xác nhận
    /// </summary>
    public class ConfirmConsumptionQuantityDto
    {
        public int OrderDetailId { get; set; }
        /// <summary>
        /// OrderComboItemId - null nếu là món lẻ, có giá trị nếu là món trong combo
        /// </summary>
        public int? OrderComboItemId { get; set; }
        public int Quantity { get; set; } // Số lượng đã lấy/xác nhận
    }

    /// <summary>
    /// Response sau khi xác nhận số lượng
    /// </summary>
    public class ConfirmConsumptionQuantityResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

