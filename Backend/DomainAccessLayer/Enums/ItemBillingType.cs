using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainAccessLayer.Enums
{
    /// <summary>
    /// Phân loại món theo cách tính tiền
    /// </summary>
    public enum ItemBillingType
    {
        /// <summary>
        /// Chưa xác định - giá trị mặc định
        /// Nếu món có value này, system sẽ fallback về KitchenPrepared
        /// </summary>
        Unspecified = 0,
        
        /// <summary>
        /// Món tính tiền theo số lượng thực tế khách sử dụng
        /// Ví dụ: Bia lon/chai, nước ngọt, khăn lạnh, khăn ướt, bia tươi
        /// Khách chỉ thanh toán số lượng đã dùng, không phải số lượng đặt
        /// </summary>
        ConsumptionBased = 1,
        
        /// <summary>
        /// Món chế biến trong bếp - phải thanh toán 100% số lượng đã đặt
        /// Ví dụ: Lẩu, steak, rau, cơm, món nóng/món nấu
        /// Nếu bếp đã nấu thì phải thanh toán đủ, không phụ thuộc số lượng dùng
        /// </summary>
        KitchenPrepared = 2
    }
}

