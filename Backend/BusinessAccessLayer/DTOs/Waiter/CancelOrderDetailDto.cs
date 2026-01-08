namespace BusinessAccessLayer.DTOs.Waiter
{
    /// <summary>
    /// Request để hủy món (chưa nấu)
    /// </summary>
    public class CancelOrderDetailDto
    {
        public int OrderDetailId { get; set; }
        /// <summary>
        /// OrderComboItemId - null nếu là món lẻ, có giá trị nếu là món trong combo
        /// </summary>
        public int? OrderComboItemId { get; set; }
        public int WaiterUserId { get; set; } // ID của waiter hủy
        public string Reason { get; set; } = string.Empty; // Lý do hủy: khách đổi ý...
    }

    /// <summary>
    /// Response sau khi hủy món
    /// </summary>
    public class CancelOrderDetailResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal? RefundAmount { get; set; } // Số tiền hoàn lại (nếu có)
    }
}

