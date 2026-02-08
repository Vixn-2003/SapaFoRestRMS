using DomainAccessLayer.Enums;

namespace BusinessAccessLayer.DTOs.OrderConfirmation
{
    /// <summary>
    /// DTO cho món trong hóa đơn xác nhận
    /// Phân biệt rõ 2 loại món: Kitchen-prepared và Consumption-based
    /// </summary>
    public class OrderItemConfirmDto
    {
        public int OrderDetailId { get; set; }
        
        public int? MenuItemId { get; set; }
        
        public int? ComboId { get; set; }
        
        public string ItemName { get; set; } = string.Empty;
        
        public string? ItemType { get; set; } // "MenuItem" hoặc "Combo"
        
        /// <summary>
        /// Số lượng đặt ban đầu
        /// </summary>
        public int QuantityOrdered { get; set; }
        
        /// <summary>
        /// Số lượng thực tế sử dụng (chỉ cho Consumption-based items)
        /// </summary>
        public int? QuantityUsed { get; set; }
        
        public decimal UnitPrice { get; set; }
        
        /// <summary>
        /// Thành tiền = QuantityUsed × UnitPrice (cho Consumption)
        /// hoặc QuantityOrdered × UnitPrice (cho Kitchen)
        /// </summary>
        public decimal TotalPrice { get; set; }
        
        /// <summary>
        /// Trạng thái món (Pending, Confirmed, Cooking, Done, Served, Removed)
        /// </summary>
        public string Status { get; set; } = "Pending";
        
        /// <summary>
        /// Loại tính tiền của món
        /// </summary>
        public ItemBillingType BillingType { get; set; }
        
        /// <summary>
        /// Món chế biến trong bếp (true) hay món tiêu hao (false)
        /// </summary>
        public bool IsKitchenItem => BillingType == ItemBillingType.KitchenPrepared;
        
        /// <summary>
        /// Món tính theo số lượng sử dụng thực tế
        /// </summary>
        public bool IsConsumptionItem => BillingType == ItemBillingType.ConsumptionBased;
        
        /// <summary>
        /// Trạng thái bếp cho Kitchen items
        /// </summary>
        public string KitchenStatus => GetKitchenStatus();
        
        /// <summary>
        /// Có thể hủy món hay không
        /// </summary>
        public bool CanCancel => IsKitchenItem && (Status == "Pending" || Status == "Confirmed" || Status == "Cooking");
        
        /// <summary>
        /// Badge color cho trạng thái bếp
        /// </summary>
        public string StatusBadgeClass => GetStatusBadgeClass();
        
        /// <summary>
        /// Background color cho row
        /// </summary>
        public string RowBackgroundClass => CanCancel ? "bg-cancelable" : "bg-non-cancelable";
        
        public string? Notes { get; set; }
        
        private string GetKitchenStatus()
        {
            if (!IsKitchenItem) return "";
            
            return Status switch
            {
                "Pending" or "Confirmed" => "📗 Chưa chế biến",
                "Cooking" => "🟠 Đang chế biến",
                "Done" or "Served" => "🔵 Đã hoàn thành",
                "Removed" => "❌ Đã hủy",
                _ => "❓ Không rõ"
            };
        }
        
        private string GetStatusBadgeClass()
        {
            if (!IsKitchenItem) return "";
            
            return Status switch
            {
                "Pending" or "Confirmed" => "badge-kitchen-notstarted",
                "Cooking" => "badge-kitchen-cooking",
                "Done" or "Served" => "badge-kitchen-done",
                "Removed" => "badge-kitchen-removed",
                _ => "badge-secondary"
            };
        }
    }
}

