using BusinessAccessLayer.Services.Interfaces;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using System;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class PayosService : IPayosService
    {
        private readonly PayOSClient _payOSClient;

        public PayosService(HttpClient httpClient, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            var clientId = config["PayOS:ClientId"];
            var apiKey = config["PayOS:ApiKey"];
            var checksumKey = config["PayOS:ChecksumKey"];

            if (string.IsNullOrWhiteSpace(clientId) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(checksumKey))
                throw new Exception("PayOS config missing.");

            _payOSClient = new PayOSClient(clientId, apiKey, checksumKey);
        }

        public async Task<string> CreatePaymentAsync(
            decimal amount,
            string orderId,
            string description,
            string returnUrl,
            string ipnUrl)
        {
            // PayOS amount thường là VND số nguyên
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be > 0.");

            if (amount > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount is too large.");

            if (string.IsNullOrWhiteSpace(orderId))
                throw new ArgumentException("orderId is required.", nameof(orderId));

            if (!long.TryParse(orderId, out var orderCode))
                throw new Exception("OrderId for PayOS must be numeric (orderCode).");

            if (string.IsNullOrWhiteSpace(returnUrl))
                throw new ArgumentException("returnUrl is required.", nameof(returnUrl));

            // PayOS SDK thường giới hạn description ngắn
            description ??= "";
            if (description.Length > 25)
                description = description.Substring(0, 25);

            // ipnUrl: với PayOSClient.PaymentRequests.CreateAsync(req) bản V2 thường KHÔNG set ipnUrl ở đây,
            // webhook/IPN sẽ cấu hình trong PayOS Dashboard. Giữ param để không phải sửa interface.
            _ = ipnUrl;

            // Tự suy ra cancelUrl nếu bạn không truyền riêng
            var cancelUrl = returnUrl.Replace("payment-result", "payment-cancel");

            var req = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = (int)decimal.Truncate(amount),
                Description = description,
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl
            };

            var res = await _payOSClient.PaymentRequests.CreateAsync(req);

            if (res == null || string.IsNullOrWhiteSpace(res.CheckoutUrl))
                throw new Exception("PayOS did not return checkoutUrl.");

            return res.CheckoutUrl;
        }
    }
}
