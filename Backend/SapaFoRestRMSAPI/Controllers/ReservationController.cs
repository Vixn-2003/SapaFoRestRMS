using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PayOS;
 // WebhookData, verifiedData
using PayOS.Models.Webhooks;
using SapaFoRestRMSAPI.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationController : ControllerBase
    {
        private readonly IReservationService _reservationService;
        private readonly OtpService _otpService;
        private readonly IMomoService _momoService;
        private readonly IPayosService _payosService;

        private readonly IConfiguration _config;
        private readonly PayOSClient _payOSClient;

        // ✅ thread-safe
        private static readonly ConcurrentDictionary<string, OtpInfo> _otpCache = new();
        private static readonly ConcurrentDictionary<string, ReservationCreateDto> _pendingReservationCache = new();

        private const decimal DEPOSIT_PER_GUEST = 1000m;

        public ReservationController(
            IReservationService reservationService,
            IMomoService momoService,
            IPayosService payosService,
            IConfiguration config)
        {
            _reservationService = reservationService;
            _momoService = momoService;
            _payosService = payosService;
            _otpService = new OtpService();

            _config = config;

            _payOSClient = new PayOSClient(
                _config["PayOS:ClientId"],
                _config["PayOS:ApiKey"],
                _config["PayOS:ChecksumKey"]
            );
        }

        // ====================== SEND OTP ======================
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] string phone)
        {
            var now = DateTime.Now;

            if (_otpCache.TryGetValue(phone, out var info))
            {
                if (info.LastSent.Date != now.Date)
                {
                    info.DailyCount = 0;
                    info.Timestamps.Clear();
                }

                info.Timestamps = info.Timestamps.Where(t => (now - t).TotalMinutes < 10).ToList();

                if (info.Timestamps.Count >= 2)
                    return BadRequest(new { message = "Gửi OTP quá 2 lần trong 10 phút." });

                if (info.DailyCount >= 3)
                    return BadRequest(new { message = "Gửi OTP quá 3 lần trong ngày." });
            }

            var otp = new Random().Next(100000, 999999).ToString();
            var expired = now.AddMinutes(5);

            if (!await _otpService.SendOtpAsync(phone, otp))
                return BadRequest(new { message = "Không gửi được OTP." });

            _otpCache.AddOrUpdate(
                phone,
                _ => new OtpInfo
                {
                    OtpCode = otp,
                    Expired = expired,
                    DailyCount = 1,
                    LastSent = now,
                    Timestamps = new List<DateTime> { now }
                },
                (_, old) =>
                {
                    old.OtpCode = otp;
                    old.Expired = expired;
                    old.DailyCount++;
                    old.LastSent = now;
                    old.Timestamps.Add(now);
                    return old;
                });

            return Ok(new { message = "OTP đã gửi", expireAt = expired });
        }

        // ====================== CONFIRM RESERVATION ======================
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmReservation([FromBody] ReservationCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });

            if (!_otpCache.TryGetValue(dto.Phone, out var otpInfo))
                return BadRequest(new { success = false, message = "Chưa gửi OTP." });

            if (DateTime.Now > otpInfo.Expired)
                return BadRequest(new { success = false, message = "OTP hết hạn." });

            if (dto.OtpCode != otpInfo.OtpCode)
                return BadRequest(new { success = false, message = "OTP không đúng." });

            decimal requiredDeposit = dto.NumberOfGuests * DEPOSIT_PER_GUEST;

            try
            {
                // ✅ lấy từ config
                string returnUrl = _config["PayOS:ReturnUrl"];
                string cancelUrl = _config["PayOS:CancelUrl"];
                string webhookUrl = _config["PayOS:WebhookUrl"]; // để bạn trả về cho FE/Log, PayOS gọi theo cấu hình dashboard

                string paymentMethod = dto.PaymentMethod?.ToUpper() ?? "PAYOS";
                string orderId;
                string orderInfo;
                string payUrl;

                if (paymentMethod == "MOMO")
                {
                    orderId = Guid.NewGuid().ToString("N");
                    orderInfo = $"Coc {dto.NumberOfGuests} khach";

                    payUrl = await _momoService.CreatePaymentAsync(requiredDeposit, orderId, orderInfo);
                }
                else
                {
                    // ✅ giảm nguy cơ trùng
                    long orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    orderId = orderCode.ToString();

                    orderInfo = "Tien coc dat ban";

                    // ipnUrl param bạn đang truyền vào service: SDK payOS thường cấu hình webhook ở dashboard,
                    // nhưng cứ truyền để giữ interface không đổi.
                    payUrl = await _payosService.CreatePaymentAsync(requiredDeposit, orderId, orderInfo, returnUrl, webhookUrl);
                    paymentMethod = "PAYOS";
                }

                _pendingReservationCache[orderId] = dto;
                _otpCache.TryRemove(dto.Phone, out _);

                return Ok(new
                {
                    success = true,
                    message = "Vui lòng thanh toán tiền cọc.",
                    orderId,
                    requiredDeposit,
                    payUrl,
                    paymentMethod,
                    returnUrl,
                    cancelUrl
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ====================== IPN MOMO ======================
        [HttpPost("momo-ipn")]
        public async Task<IActionResult> MomoIpn([FromBody] MomoIpnRequest ipn)
        {
            if (!_pendingReservationCache.TryGetValue(ipn.orderId, out _))
                return Ok(new { message = "orderId not found" });

            if (ipn.resultCode != 0)
                return Ok(new { message = "payment failed" });

            return await HandlePaymentSuccessAsync(
                ipn.orderId,
                decimal.Parse(ipn.amount),
                "MOMO",
                ipn.transId);
        }

        // ====================== IPN PAYOS (VERIFY SIGNATURE) ======================
        // ✅ nhận WebhookData đúng theo SDK, rồi VerifyAsync
        [HttpPost("payos-ipn")]
        public async Task<IActionResult> PayosIpn()
        {
            Console.WriteLine("=== PAYOS IPN HIT (RAW) ===");

            string raw;
            using (var reader = new StreamReader(Request.Body))
                raw = await reader.ReadToEndAsync();

            Console.WriteLine("RAW=" + raw);

            if (string.IsNullOrWhiteSpace(raw))
                return BadRequest(new { message = "empty body" });

            Webhook? webhook;
            try
            {
                webhook = JsonSerializer.Deserialize<Webhook>(raw, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("DESERIALIZE FAIL: " + ex.Message);
                return BadRequest(new { message = "invalid json" });
            }

            if (webhook?.Data == null)
            {
                Console.WriteLine("-> TEST WEBHOOK (no data)");
                return Ok(new { message = "webhook test ok" });
            }

            try
            {
                var verified = await _payOSClient.Webhooks.VerifyAsync(webhook);
                Console.WriteLine("-> VERIFY OK");

                var orderId = verified.OrderCode.ToString();
                Console.WriteLine("orderId=" + orderId);

                if (!_pendingReservationCache.TryGetValue(orderId, out _))
                {
                    Console.WriteLine("-> orderId NOT FOUND in pending cache");
                    // nên trả 200 để PayOS không đánh fail, nhưng bạn sẽ biết nguyên nhân
                    return Ok(new { message = "orderId not found" });
                }

                Console.WriteLine("-> CALL HandlePaymentSuccessAsync");
                return await HandlePaymentSuccessAsync(orderId,
                    Convert.ToDecimal(verified.Amount),
                    "PAYOS",
                    verified.Reference);
            }
            catch (Exception ex)
            {
                Console.WriteLine("-> VERIFY FAIL: " + ex.Message);

                // LÚC DEV: trả 400 để PayOS dashboard biết webhook lỗi thật
                // Khi demo xong có thể đổi lại Ok()
                return BadRequest(new { message = "verify failed", detail = ex.Message });
            }
        }



        // ============== API khách hàng lấy danh sách đặt bàn ==============
        [HttpGet("{customerId}")]
        public async Task<IActionResult> GetReservationsByCustomer(int customerId)
        {
            var result = await _reservationService.GetReservationsByCustomerAsync(customerId);
            return Ok(result);
        }

        [HttpPut("Update/{id}")]
        public async Task<IActionResult> UpdateReservation(int id, [FromBody] ReservationUpdateDto dto)
        {
            try
            {
                var result = await _reservationService.UpdateReservationAsync(id, dto);
                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("Cancel/{id}")]
        public async Task<IActionResult> CancelReservation(int id)
        {
            try
            {
                var result = await _reservationService.CancelReservationByCustomerAsync(id);
                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ====================== HANDLE SUCCESS ======================
        private async Task<IActionResult> HandlePaymentSuccessAsync(
            string orderId,
            decimal amount,
            string method,
            string transId)
        {
            if (!_pendingReservationCache.TryGetValue(orderId, out var dto))
                return Ok(new { message = "orderId not found" });

            var reservation = await _reservationService.CreateReservationAsync(dto);

            var deposit = new DomainAccessLayer.Models.ReservationDeposit
            {
                ReservationId = reservation.ReservationId,
                Amount = amount,
                PaymentMethod = method,
                DepositCode = transId,
                DepositDate = DateTime.Now,
                Notes = $"Thanh toán đặt cọc qua {method}"
            };

            await _reservationService.AddDepositAsync(reservation.ReservationId, deposit);

            reservation.DepositPaid = true;
            reservation.DepositAmount = amount;
            reservation.TotalDepositPaid = amount;
            reservation.RequireDeposit = true;
            reservation.Status = "Pending";

            await _reservationService.UpdateReservationDepositStatusAsync(reservation);

            _pendingReservationCache.TryRemove(orderId, out _);

            return Ok(new { message = "reservation created", reservationId = reservation.ReservationId });
        }
    }

    public class OtpInfo
    {
        public string OtpCode { get; set; }
        public DateTime Expired { get; set; }
        public int DailyCount { get; set; }
        public DateTime LastSent { get; set; }
        public List<DateTime> Timestamps { get; set; } = new();
    }
}
