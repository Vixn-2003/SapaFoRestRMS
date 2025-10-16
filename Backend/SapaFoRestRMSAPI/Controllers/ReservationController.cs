using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationController : ControllerBase
    {
        private readonly IReservationService _reservationService;
        private readonly OtpService _otpService;

        private static Dictionary<string, OtpInfo> _otpCache = new();

        public ReservationController(IReservationService reservationService)
        {
            _reservationService = reservationService;
            _otpService = new OtpService();
        }

        //  GỬI OTP
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

                // Xóa lần gửi đã quá 10 phút
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

        //  XÁC NHẬN & TẠO ĐẶT BÀN
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmReservation([FromBody] ReservationCreateDto dto)
        {
            if (!_otpCache.ContainsKey(dto.Phone))
                return BadRequest(new { message = "Chưa gửi OTP đến số này." });

            var info = _otpCache[dto.Phone];

            if (DateTime.Now > info.Expired)
                return BadRequest(new { message = "Mã OTP đã hết hạn." });

            if (dto.OtpCode != info.OtpCode)
                return BadRequest(new { message = "Mã OTP không chính xác." });

            var reservation = await _reservationService.CreateReservationAsync(dto);
            if (reservation == null)
            {
                string formattedDate = dto.ReservationDate.Date.ToString("dd/MM/yyyy");
                string timeSlot = dto.ReservationTime.Hour switch
                {
                    >= 6 and < 10 => "Ca sáng",
                    >= 10 and < 14 => "Ca trưa",
                    _ => "Ca tối"
                };
                return BadRequest(new
                {
                    message = $"Số điện thoại đã đặt bàn cho {formattedDate} ({timeSlot}). Vui lòng kiểm tra hoặc đổi thời gian."
                });
            }

            _otpCache.Remove(dto.Phone); // xoá sau kh

            return Ok(new
            {
                reservationId = reservation.ReservationId,
                message = "Đặt bàn thành công",
                timeSlot = reservation.TimeSlot
            });
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
