namespace BusinessAccessLayer.DTOs.Waiter
{
    /// <summary>
    /// Request để đánh dấu món đã phục vụ (lấy món)
    /// </summary>
    public class MarkAsServedDto
    {
        public int OrderDetailId { get; set; }
        /// <summary>
        /// OrderComboItemId - null nếu là món lẻ, có giá trị nếu là món trong combo
        /// </summary>
        public int? OrderComboItemId { get; set; }
        public int WaiterUserId { get; set; } // ID của waiter lấy món
        public int Quantity { get; set; } // Số lượng lấy (mặc định = Quantity của OrderDetail nếu không chỉ định)
    }

    /// <summary>
    /// Response sau khi đánh dấu đã phục vụ
    /// </summary>
    public class MarkAsServedResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

