using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IPayosService
    {
        /// <summary>
        /// Tạo link thanh toán PayOS
        /// </summary>
        /// <param name="amount">Số tiền (VND)</param>
        /// <param name="orderId">Mã order nội bộ (GUID string)</param>
        /// <param name="description">Mô tả hiển thị cho khách + chứa orderId để map IPN</param>
        /// <param name="returnUrl">URL frontend trả về sau khi thanh toán</param>
        /// <param name="ipnUrl">Webhook URL PayOS gọi ngược về</param>
        Task<string> CreatePaymentAsync(decimal amount, string orderId, string description, string returnUrl, string ipnUrl);
    }
}
