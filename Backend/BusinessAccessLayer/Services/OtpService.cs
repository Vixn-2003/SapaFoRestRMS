using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class OtpService
    {
        private readonly string _apiKey = "e1U7HuMJtajomx5s8higT05vxieVGyOt"; // Access Token của bạn
        private readonly HttpClient _httpClient;
        private readonly string _logFilePath;
        public OtpService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.speedsms.vn/index.php/sms/");
            var byteArray = Encoding.ASCII.GetBytes(_apiKey + ":x");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            var logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            _logFilePath = Path.Combine(logDir, "otp_log.txt");
        }

        public async Task<bool> SendOtpAsync(string phone, string otp)
        {
            try
            {
                // Theo tài liệu: type = 2, sender = "" cho OTP
                var payload = new
                {
                    to = new string[] { phone },
                    content = $"[SapaFoRest] Mã xác nhận đặt bàn của bạn là: {otp}",
                    type = 2,
                    sender = "" // bắt buộc có nhưng để trống
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("send", content);
                var result = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[SpeedSMS Response] {result}");

                using var doc = JsonDocument.Parse(result);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var statusProp))
                {
                    var status = statusProp.GetString();

                    if (status == "success")
                    {
                        Console.WriteLine($" OTP đã được gửi đến {phone}");
                        return true;
                    }

                    if (status == "error")
                    {
                        var msg = root.GetProperty("message").GetString();
                        Console.WriteLine($" SpeedSMS Error: {msg}");

                        // Nếu gặp "sender not found" thì in ra demo
                        if (msg.Contains("sender not found", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"[DEMO MODE] OTP {otp} (chưa bật kênh type=2)");
                            LogToFile($"[DEMO MODE] OTP {otp} (chưa bật kênh type=2)");
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SpeedSMS Exception] {ex.Message}");
                Console.WriteLine($"[DEMO MODE] OTP {otp}");
                return true; // fallback
            }

            return false;
        }

        /// <summary>
        /// Ghi log ra file với timestamp
        /// </summary>
        private void LogToFile(string message)
        {
            try
            {
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(_logFilePath, line, Encoding.UTF8);
            }
            catch
            {
                // Nếu ghi file lỗi thì bỏ qua, tránh crash
            }

            // Ghi thêm ra console (để xem nhanh trên log server)
            Console.WriteLine(message);
        }
    }
}
