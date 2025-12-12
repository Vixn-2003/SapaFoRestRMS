using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SapaFoRestRMSAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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

        // Cache OTP theo số điện thoại
        private static Dictionary<string, OtpInfo> _otpCache = new();

        // Cache đơn đặt bàn đang chờ thanh toán (key = orderId của MoMo)
        private static Dictionary<string, ReservationCreateDto> _pendingReservationCache = new();

        private const decimal DEPOSIT_PER_GUEST = 50000m;

        public ReservationController(
            IReservationService reservationService,
            IMomoService momoService)
        {
            _reservationService = reservationService;
            _momoService = momoService;
            _otpService = new OtpService();
        }

        // ================== GỬI OTP ==================
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] string phone)
        {
            var now = DateTime.Now;

            if (_otpCache.ContainsKey(phone))
            {
                var info = _otpCache[phone];

                // Reset theo ngày
                if (info.LastSent.Date != now.Date)
                {
                    info.DailyCount = 0;
                    info.LastSent = now;
                    info.Timestamps.Clear();
                }

                // Giữ lại các lần gửi trong 10 phút gần nhất
                info.Timestamps = info.Timestamps
                    .Where(t => (now - t).TotalMinutes < 10)
                    .ToList();

                // Giới hạn 2 lần/10 phút
                if (info.Timestamps.Count >= 2)
                    return BadRequest(new { message = "Bạn đã gửi OTP quá 2 lần trong 10 phút, vui lòng thử lại sau." });

                // Giới hạn 3 lần/ngày
                if (info.DailyCount >= 3)
                    return BadRequest(new { message = "Bạn đã gửi OTP quá 3 lần trong ngày, vui lòng thử lại vào ngày mai." });
            }

            var otp = new Random().Next(100000, 999999).ToString();
            var expired = now.AddMinutes(5);

            var sent = await _otpService.SendOtpAsync(phone, otp);
            if (!sent)
                return BadRequest(new { message = "Không thể gửi OTP, vui lòng thử lại." });

            if (!_otpCache.ContainsKey(phone))
            {
                _otpCache[phone] = new OtpInfo
                {
                    OtpCode = otp,
                    Expired = expired,
                    DailyCount = 1,
                    LastSent = now,
                    Timestamps = new List<DateTime> { now }
                };
            }
            else
            {
                var info = _otpCache[phone];
                info.OtpCode = otp;
                info.Expired = expired;
                info.DailyCount++;
                info.LastSent = now;
                info.Timestamps.Add(now);
            }

            Console.WriteLine($"[DEBUG OTP] {phone}: {otp}");

            return Ok(new
            {
                message = "OTP đã được gửi.",
                expireAt = expired
            });
        }

        // ============= CONFIRM: validate + check trùng + tạo link MoMo =============
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmReservation([FromBody] ReservationCreateDto dto)
        {
            // Validate DataAnnotations
            if (!ModelState.IsValid)
            {
                var error = ModelState.Values
                                      .SelectMany(v => v.Errors)
                                      .FirstOrDefault()?.ErrorMessage;

                return BadRequest(new { success = false, message = error });
            }

            // Ngày trong quá khứ
            if (dto.ReservationDate.Date < DateTime.Today)
                return BadRequest(new { success = false, message = "Ngày đặt bàn không được ở trong quá khứ." });

            // Nếu đặt hôm nay thì giờ phải lớn hơn hiện tại
            if (dto.ReservationDate.Date == DateTime.Today &&
                dto.ReservationTime < DateTime.Now)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Giờ đặt bàn không hợp lệ. Không thể đặt giờ đã qua."
                });
            }

            // Kiểm tra OTP
            if (!_otpCache.ContainsKey(dto.Phone))
                return BadRequest(new { success = false, message = "Chưa gửi OTP đến số này." });

            var info = _otpCache[dto.Phone];

            if (DateTime.Now > info.Expired)
                return BadRequest(new { success = false, message = "Mã OTP đã hết hạn." });

            if (dto.OtpCode != info.OtpCode)
                return BadRequest(new { success = false, message = "Mã OTP không chính xác." });

            // Tính ca (slot)
            string GetTimeSlot(DateTime reservationTime)
            {
                var hour = reservationTime.Hour;
                if (hour >= 6 && hour < 10) return "Ca sáng";
                if (hour >= 10 && hour < 14) return "Ca trưa";
                return "Ca tối";
            }

            var fullDateTime = dto.ReservationDate.Date + dto.ReservationTime.TimeOfDay;
            var timeSlot = GetTimeSlot(fullDateTime);

            // ========== CHECK TRÙNG ĐƠN (cùng số, cùng ngày, cùng ca) ==========
            var hasExisting = await _reservationService.HasExistingReservationAsync(
                dto.Phone,
                dto.ReservationDate.Date,
                timeSlot);

            if (hasExisting)
            {
                string formattedDate = dto.ReservationDate.ToString("dd/MM/yyyy");
                return BadRequest(new
                {
                    success = false,
                    message = $"Số điện thoại đã đặt bàn cho {formattedDate} ({timeSlot}). Vui lòng kiểm tra hoặc đổi thời gian bằng cách liên hệ qua hotline để được hỗ trợ."
                });
            }
            // ====================================================================

            decimal requiredDeposit = dto.NumberOfGuests * DEPOSIT_PER_GUEST;

            string orderId = Guid.NewGuid().ToString("N");
            string orderInfo = $"Dat coc dat ban {dto.CustomerName} - {dto.Phone}";

            string payUrl;
            try
            {
                payUrl = await _momoService.CreatePaymentAsync(requiredDeposit, orderId, orderInfo);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Không tạo được link thanh toán MoMo: " + ex.Message });
            }

            // Lưu DTO vào cache, chờ IPN xác nhận thanh toán
            _pendingReservationCache[orderId] = dto;
            // Xóa OTP sau khi dùng
            _otpCache.Remove(dto.Phone);

            return Ok(new
            {
                success = true,
                message = "Vui lòng thanh toán tiền cọc để hoàn tất đặt bàn.",
                orderId = orderId,
                requiredDeposit = requiredDeposit,
                payUrl = payUrl,
                timeSlot = timeSlot
            });
        }

        // ================== IPN từ MoMo ==================
        [HttpPost("momo-ipn")]
        public async Task<IActionResult> MomoIpn([FromBody] MomoIpnRequest ipn)
        {
            // TODO: verify signature khi chạy thật

            if (!_pendingReservationCache.TryGetValue(ipn.orderId, out var dto))
            {
                return Ok(new { message = "orderId not found" });
            }

            if (ipn.resultCode != 0)
            {
                _pendingReservationCache.Remove(ipn.orderId);
                return Ok(new { message = "payment failed" });
            }

            var reservation = await _reservationService.CreateReservationAsync(dto);
            if (reservation == null)
            {
                _pendingReservationCache.Remove(ipn.orderId);
                return Ok(new { message = "reservation duplicated or failed" });
            }

            decimal depositAmount = decimal.Parse(ipn.amount);

            var deposit = new DomainAccessLayer.Models.ReservationDeposit
            {
                ReservationId = reservation.ReservationId,
                Amount = depositAmount,
                PaymentMethod = "MOMO",
                DepositCode = ipn.transId,
                DepositDate = DateTime.Now,
                Notes = "Thanh toán đặt cọc qua MoMo"
            };

            await _reservationService.AddDepositAsync(reservation.ReservationId, deposit);

            reservation.RequireDeposit = true;
            reservation.DepositAmount = depositAmount;
            reservation.TotalDepositPaid = depositAmount;
            reservation.DepositPaid = true;
            reservation.Status = "Pending";

            await _reservationService.UpdateReservationDepositStatusAsync(reservation);

            _pendingReservationCache.Remove(ipn.orderId);

            return Ok(new { message = "payment success, reservation created" });
        }

        // ============== API khách hàng lấy danh sách đặt bàn ==============
        [HttpGet("{customerId}")]
        public async Task<IActionResult> GetReservationsByCustomer(int customerId)
        {
            var result = await _reservationService.GetReservationsByCustomerAsync(customerId);
            return Ok(result);
        }

        // ============== Cập nhật đặt bàn (admin/staff?) ==============
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

        // ============== Khách hủy đặt bàn ==============
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
    }

    public class OtpInfo
    {
        public string OtpCode { get; set; } = null!;
        public DateTime Expired { get; set; }
        public int DailyCount { get; set; }
        public DateTime LastSent { get; set; }
        public List<DateTime> Timestamps { get; set; } = new();
    }
}
