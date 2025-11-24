using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.Payment
{
    /// <summary>
    /// Yêu cầu xác nhận lại các món khách đã dùng trước khi thanh toán.
    /// </summary>
    public class CustomerConfirmRequestDto
    {
        public int OrderId { get; set; }

        public List<CustomerConfirmedItemDto> Items { get; set; } = new();

        public string? Notes { get; set; }
    }

    public class CustomerConfirmedItemDto
    {
        /// <summary>
        /// ID của chi tiết đơn hàng (OrderDetailId)
        /// </summary>
        public int OrderDetailId { get; set; }

        /// <summary>
        /// Số lượng khách thực tế đã dùng.
        /// </summary>
        public int QuantityUsed { get; set; }

        /// <summary>
        /// Đánh dấu món bị hủy/bỏ.
        /// </summary>
        public bool IsRemoved { get; set; }
    }
}

