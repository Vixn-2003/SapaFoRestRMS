using System;
using System.Collections.Generic;

namespace BusinessAccessLayer.DTOs.Waiter
{
    /// <summary>
    /// DTO cho màn hình theo dõi tiến độ phục vụ của Waiter
    /// </summary>
    public class WaiterOrderTrackingDto
    {
        public int ProcessingCount { get; set; } // Đang xử lý
        public int WaitingKitchenCount { get; set; } // Chờ bếp
        public int CookingCount { get; set; } // Đang nấu
        public int ReadyCount { get; set; } // Sẵn sàng lấy
        public int TotalCount { get; set; } // Tổng số

        public List<OrderTrackingGroupDto> OrderGroups { get; set; } = new();
    }

    /// <summary>
    /// Nhóm orders theo bàn/khu vực
    /// </summary>
    public class OrderTrackingGroupDto
    {
        public string OrderNumber { get; set; } = string.Empty; // A2-02
        public string AreaName { get; set; } = string.Empty; // Khu A1
        public string TableNumber { get; set; } = string.Empty; // Bàn số
        public int NumberOfGuests { get; set; } // Số lượng khách
        public List<OrderTrackingItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Chi tiết món trong order tracking
    /// </summary>
    public class OrderTrackingItemDto
    {
        public int OrderDetailId { get; set; }
        /// <summary>
        /// OrderComboItemId - null nếu là món lẻ, có giá trị nếu là món trong combo
        /// </summary>
        public int? OrderComboItemId { get; set; }
        public int OrderId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty; // Pending, Cooking, Ready, Done, Cancelled, Returned
        public string? Notes { get; set; } // Ghi chú từ khách
        public bool IsUrgent { get; set; } // Đánh dấu làm gấp
        public string? UrgentReason { get; set; } // Lý do làm gấp
        public DateTime OrderTime { get; set; } // Thời gian order
        public int WaitingMinutes { get; set; } // Số phút đã chờ
        public DateTime? StartedAt { get; set; } // Thời gian bắt đầu nấu
        public DateTime? ReadyAt { get; set; } // Thời gian sẵn sàng
        public DateTime? ServedAt { get; set; } // Thời gian đã phục vụ
        public bool CanCancel { get; set; } // Có thể hủy (chưa nấu)
        public bool CanReturn { get; set; } // Có thể trả (đã nấu/đã ra)
        public bool CanRequestUrgent { get; set; } // Có thể yêu cầu làm gấp
        public bool IsSplit { get; set; } // Đã được tách từ order detail gốc (bếp phó nấu một phần)
        
        /// <summary>
        /// BillingType của món: 0=Unspecified, 1=ConsumptionBased, 2=KitchenPrepared
        /// Món có BillingType = 1 (ConsumptionBased) có thể tăng/giảm số lượng kể cả sau khi xác nhận
        /// </summary>
        public int? BillingType { get; set; }
        
        /// <summary>
        /// True nếu là món ConsumptionBased (BillingType = 1) - có thể tăng/giảm số lượng
        /// </summary>
        public bool IsConsumptionBased => BillingType == 1;
        
        /// <summary>
        /// Số lượng thực tế đã sử dụng (cho ConsumptionBased items)
        /// </summary>
        public int? QuantityUsed { get; set; }
        
        /// <summary>
        /// True nếu có thể xác nhận số lượng ngay (cho món ConsumptionBased, không cần chờ Ready)
        /// </summary>
        public bool CanConfirmQuantity { get; set; }
    }
}

