namespace BusinessAccessLayer.DTOs.OrderConfirmation
{
    /// <summary>
    /// Request DTO cho việc xác nhận hóa đơn
    /// </summary>
    public class ConfirmOrderRequestDto
    {
        public int OrderId { get; set; }
        
        /// <summary>
        /// Danh sách món consumption với số lượng sử dụng
        /// </summary>
        public List<ConsumptionItemUpdateDto> ConsumptionItems { get; set; } = new();
        
        /// <summary>
        /// Ghi chú xác nhận
        /// </summary>
        public string? Notes { get; set; }
        
        /// <summary>
        /// ID nhân viên xác nhận
        /// </summary>
        public int? ConfirmedByStaffId { get; set; }
    }
    
    /// <summary>
    /// DTO cho cập nhật số lượng sử dụng của món consumption
    /// </summary>
    public class ConsumptionItemUpdateDto
    {
        public int OrderDetailId { get; set; }
        
        /// <summary>
        /// Số lượng thực tế sử dụng
        /// </summary>
        public int QuantityUsed { get; set; }
    }
}

