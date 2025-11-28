namespace BusinessAccessLayer.DTOs.OrderConfirmation
{
    /// <summary>
    /// Request DTO cho việc hủy món
    /// </summary>
    public class CancelItemRequestDto
    {
        public int OrderId { get; set; }
        
        public int OrderDetailId { get; set; }
        
        /// <summary>
        /// Lý do hủy món
        /// </summary>
        public string Reason { get; set; } = string.Empty;
        
        /// <summary>
        /// ID nhân viên thực hiện
        /// </summary>
        public int? StaffId { get; set; }
    }
}

