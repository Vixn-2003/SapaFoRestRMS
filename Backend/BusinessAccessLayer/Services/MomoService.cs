using BusinessAccessLayer.Services.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class MomoService : IMomoService
    {
        private readonly HttpClient _httpClient;
        private readonly MomoOptions _options;

        public MomoService(IOptions<MomoOptions> options)
        {
            _options = options.Value;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://test-payment.momo.vn/v2/gateway/api/")
            };
        }

        public async Task<string> CreatePaymentAsync(decimal amount, string orderId, string orderInfo)
        {
            long amountLong = (long)amount;
            string requestId = Guid.NewGuid().ToString("N");
            string requestType = "captureWallet";
            string extraData = ""; // tạm thời để rỗng

            string rawHash =
                $"accessKey={_options.AccessKey}" +
                $"&amount={amountLong}" +
                $"&extraData={extraData}" +
                $"&ipnUrl={_options.IpnUrl}" +
                $"&orderId={orderId}" +
                $"&orderInfo={orderInfo}" +
                $"&partnerCode={_options.PartnerCode}" +
                $"&redirectUrl={_options.RedirectUrl}" +
                $"&requestId={requestId}" +
                $"&requestType={requestType}";

            string signature = CreateSignature(rawHash, _options.SecretKey);

            var payload = new
            {
                partnerCode = _options.PartnerCode,
                partnerName = "SapaFoRest",
                storeId = "SapaFoRestStore",
                requestId = requestId,
                amount = amountLong,              // <--- để dạng số, không ToString()
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = _options.RedirectUrl,
                ipnUrl = _options.IpnUrl,
                requestType = requestType,
                extraData = extraData,
                lang = "vi",
                signature = signature
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("create", content);
            var result = await response.Content.ReadAsStringAsync();

            Console.WriteLine("[MOMO RAW RESPONSE] " + result);   // <--- LOG

            using var doc = JsonDocument.Parse(result);
            var root = doc.RootElement;

            // Lấy resultCode + message để debug
            int resultCode = root.GetProperty("resultCode").GetInt32();
            string? message = root.GetProperty("message").GetString();

            if (resultCode != 0)
            {
                // ví dụ: sai chữ ký, sai amount, sai partnerCode...
                throw new Exception($"MoMo trả về lỗi resultCode={resultCode}, message={message}");
            }

            if (root.TryGetProperty("payUrl", out var payUrlProp))
            {
                var payUrl = payUrlProp.GetString();
                if (!string.IsNullOrEmpty(payUrl))
                    return payUrl;
            }

            // Đến được đây là MoMo trả resultCode=0 nhưng không có payUrl
            throw new Exception($"MoMo không trả về payUrl. message={message}, raw={result}");
        }


        private string CreateSignature(string rawData, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
