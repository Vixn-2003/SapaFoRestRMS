using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using WebSapaForestForStaff.Models;
using WebSapaForestForStaff.DTOs;
namespace WebSapaForestForStaff.Controllers
{
    public class ReservationStaffController : Controller
    {
        private readonly HttpClient _client;
       
        public ReservationStaffController(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7096/api/");
        }
 
        public async Task<IActionResult> Index(
             string? status,
             string? customerName,
             string? phone,
             DateTime? reservationDate,
             string? timeSlot,
             int page = 1,
             int pageSize = 10)
        {
            // Build query string safely
            var queryParts = new List<string>();
            if (!string.IsNullOrEmpty(status)) queryParts.Add($"status={Uri.EscapeDataString(status)}");
            if (!string.IsNullOrEmpty(customerName)) queryParts.Add($"customerName={Uri.EscapeDataString(customerName)}");
            if (!string.IsNullOrEmpty(phone)) queryParts.Add($"phone={Uri.EscapeDataString(phone)}");
            if (reservationDate.HasValue) queryParts.Add($"date={reservationDate:yyyy-MM-dd}");
            if (!string.IsNullOrEmpty(timeSlot)) queryParts.Add($"timeSlot={Uri.EscapeDataString(timeSlot)}");
            queryParts.Add($"page={page}");
            queryParts.Add($"pageSize={pageSize}");

            var queryString = string.Join("&", queryParts);
            var response = await _client.GetAsync($"ReservationStaff/reservations/pending-confirmed?{queryString}");

            if (!response.IsSuccessStatusCode)
            {
                // trả model rỗng để view xử lý
                var empty = new ReservationListViewModel
                {
                    TotalCount = 0,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = 0,
                    Data = new List<ReservationStaffViewModel>()
                };
                // giữ lại filter
                ViewBag.Status = status;
                ViewBag.CustomerName = customerName;
                ViewBag.Phone = phone;
                ViewBag.ReservationDate = reservationDate?.ToString("yyyy-MM-dd");
                ViewBag.TimeSlot = timeSlot;
                return View(empty);
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ReservationListViewModel>(json)
                         ?? new ReservationListViewModel { Data = new List<ReservationStaffViewModel>() };

            // Giữ lại filter để hiển thị lại trên view
            ViewBag.Status = status;
            ViewBag.CustomerName = customerName;
            ViewBag.Phone = phone;
            ViewBag.ReservationDate = reservationDate?.ToString("yyyy-MM-dd");
            ViewBag.TimeSlot = timeSlot;

            return View(result);
        }


        public async Task<IActionResult> AssignTables(int id)
        {
            var resResponse = await _client.GetAsync($"ReservationStaff/reservations/{id}");
            if (!resResponse.IsSuccessStatusCode)
                return NotFound();

            var resJson = await resResponse.Content.ReadAsStringAsync();
            var reservation = JsonConvert.DeserializeObject<ReservationStaffViewModel>(resJson);

            if (reservation == null) return NotFound();


            var tableResponse = await _client.GetAsync("ReservationStaff/tables/by-area-all");
            var tableJson = await tableResponse.Content.ReadAsStringAsync();
            var areas = JsonConvert.DeserializeObject<List<AreaViewModel>>(tableJson);

            var bookedResponse = await _client.GetAsync(
                $"ReservationStaff/tables/booked?reservationDate={reservation.ReservationDate:yyyy-MM-dd}&timeSlot={reservation.TimeSlot}");
            var bookedJson = await bookedResponse.Content.ReadAsStringAsync();
            var bookedData = JsonConvert.DeserializeObject<BookedTableResult>(bookedJson);
            var bookedTableIds = bookedData?.BookedTableIds ?? new List<int>();

            var suggestResponse = await _client.GetAsync(
                $"ReservationStaff/tables/suggest-by-areas?reservationDate={reservation.ReservationDate:yyyy-MM-dd}" +
                $"&timeSlot={reservation.TimeSlot}&numberOfGuests={reservation.NumberOfGuests}&currentReservationId={reservation.ReservationId}"
            );
            var suggestJson = await suggestResponse.Content.ReadAsStringAsync();
            var suggestData = JsonConvert.DeserializeObject<SuggestTableResult>(suggestJson);

            // Gộp cả single + combo
            var suggestedTableIdsByArea = new Dictionary<int, List<int>>();
            if (suggestData?.Areas != null)
            {
                foreach (var area in suggestData.Areas)
                {
                    var singleIds = area.SuggestedSingleTables?.Select(t => t.TableId).ToList() ?? new List<int>();
                    var comboIds = area.SuggestedCombos?
                        .SelectMany(c => c.Select(t => t.TableId))
                        .Distinct()
                        .ToList() ?? new List<int>();

                    suggestedTableIdsByArea[area.AreaId] = singleIds.Union(comboIds).ToList();
                }
            }
            var bookedInforResponse = await _client.GetAsync(
    $"ReservationStaff/tables/booked-with-time?reservationDate={reservation.ReservationDate:yyyy-MM-dd}&timeSlot={reservation.TimeSlot}");

            var bookedInforJson = await bookedInforResponse.Content.ReadAsStringAsync();
            var bookedInforData = JsonConvert.DeserializeObject<BookedTableResultWithTime>(bookedInforJson);

            // ✅ Danh sách bàn & thời gian
            var bookedTablesWithTime = bookedInforData?.BookedTables ?? new List<BookedTableDetailDto>();
            ViewBag.BookedTablesWithTime = bookedTablesWithTime;

            ViewBag.BookedTableIds = bookedTableIds;
            ViewBag.Reservation = reservation;
            ViewBag.Areas = areas;
            ViewBag.SuggestedTableIdsByArea = suggestedTableIdsByArea;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AssignTablesPost(int ReservationId, List<int> TableIds, bool RequireDeposit, decimal? DepositAmount)
        {
            if (TableIds == null || !TableIds.Any())
            {
                TempData["Error"] = "Bạn phải chọn ít nhất 1 bàn!";
                return RedirectToAction("AssignTables", new { id = ReservationId });
            }

            if (RequireDeposit && (!DepositAmount.HasValue || DepositAmount.Value <= 0))
            {
                TempData["Error"] = "Bạn phải nhập số tiền đặt cọc hợp lệ!";
                return RedirectToAction("AssignTables", new { id = ReservationId });
            }

            // Tạo DTO gửi API
            var dto = new AssignTableDto
            {
                ReservationId = ReservationId,
                TableIds = TableIds,
                RequireDeposit = RequireDeposit,
                DepositAmount = DepositAmount,
                StaffId = 3, // hoặc lấy từ session/login
                ConfirmBooking = true
            };

            var content = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
            var res = await _client.PostAsync("ReservationStaff/assign-tables", content);

            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                TempData["Error"] = $"Gán bàn thất bại: {err}";
                return RedirectToAction("AssignTables", new { id = ReservationId });
            }

            TempData["Success"] = "Gán bàn thành công!";
            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> ResetTables(int reservationId)
        {
            var res = await _client.PostAsync($"ReservationStaff/reset-tables/{reservationId}", null);

            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                TempData["Error"] = $"Reset bàn thất bại: {err}";
            }
            else
            {
                TempData["Success"] = "Đã reset bàn thành công!";
            }

            return RedirectToAction("AssignTables", new { id = reservationId });
        }
        // hủy đơn
        [HttpPost]
        public async Task<IActionResult> CancelReservation(int id, bool refund)
        {
            var response = await _client.PutAsync(
                $"ReservationStaff/cancel/{id}?refund={refund.ToString().ToLower()}",
                null
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["CancelReservationError"] = $"❌ Hủy đơn thất bại: {error}";
            }
            else
            {
                TempData["CancelReservationSuccess"] = refund
                    ? "✅ Đã hủy đơn và hoàn cọc cho khách hàng."
                    : "✅ Đã hủy đơn (không hoàn cọc).";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult CreateReservation()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReservation(ReservationViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Gửi đúng API "ReservationStaff/add"
            var dto = new
            {
                CustomerName = model.CustomerName,
                Phone = model.Phone,
                ReservationDate = model.ReservationDate,
                ReservationTime = model.ReservationTime,
                NumberOfGuests = model.NumberOfGuests,
                Notes = model.Notes
            };

            var jsonContent = JsonConvert.SerializeObject(dto);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("ReservationStaff/add", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "✅ Tạo đơn đặt bàn thành công!";
                return RedirectToAction("Index");
            }
            else
            {
                // Lấy message lỗi từ API (ví dụ "Khách hàng đã có đơn trong khung giờ này")
                try
                {
                    dynamic errorData = JsonConvert.DeserializeObject(responseBody);
                    string message = errorData?.message ?? "Không thể tạo đơn đặt bàn. Vui lòng thử lại.";
                    ModelState.AddModelError("", message);
                }
                catch
                {
                    ModelState.AddModelError("", "Không thể tạo đơn đặt bàn. Vui lòng thử lại.");
                }

                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> EditReservation(int id)
        {
            var response = await _client.GetAsync($"ReservationStaff/reservations/{id}");
            if (!response.IsSuccessStatusCode)
                return NotFound();

            var json = await response.Content.ReadAsStringAsync();
            var reservation = JsonConvert.DeserializeObject<ReservationStaffViewModel>(json);
            if (reservation == null)
                return NotFound();

            // Chuyển dữ liệu sang DTO cho form
            var model = new ReservationUpdateViewModel
            {
                ReservationId = reservation.ReservationId,
                ReservationDate = reservation.ReservationDate,
                ReservationTime = reservation.ReservationTime,
                NumberOfGuests = reservation.NumberOfGuests,
                Notes = reservation.Notes
            };

            return View("EditReservation", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReservationPost(ReservationUpdateViewModel model)
        {
            if (!ModelState.IsValid)
                return View("EditReservation", model);

            var dto = new
            {
                model.ReservationDate,
                model.ReservationTime,
                model.NumberOfGuests,
                model.Notes
            };

            var content = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
            var response = await _client.PutAsync($"ReservationStaff/update/{model.ReservationId}", content);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "✅ Cập nhật đơn đặt bàn thành công!";
                return RedirectToAction("AssignTables", new { id = model.ReservationId });

            }
            else
            {
                try
                {
                    dynamic error = JsonConvert.DeserializeObject(responseBody);
                    TempData["Error"] = $"❌ Cập nhật thất bại: {error?.message ?? "Lỗi không xác định"}";
                }
                catch
                {
                    TempData["Error"] = "❌ Cập nhật thất bại. Vui lòng thử lại.";
                }

                return View("EditReservation", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendDepositRequest([FromBody] DepositRequestDto dto)
        {
            if (dto.DepositAmount <= 0)
                return Json(new { success = false, message = "Số tiền đặt cọc không hợp lệ." });

            // Lấy thông tin đặt bàn
            var resResponse = await _client.GetAsync($"ReservationStaff/reservations/{dto.ReservationId}");
            if (!resResponse.IsSuccessStatusCode)
                return Json(new { success = false, message = "Không tìm thấy thông tin đặt bàn." });

            var resJson = await resResponse.Content.ReadAsStringAsync();
            var reservation = JsonConvert.DeserializeObject<ReservationStaffViewModel>(resJson);
            if (reservation == null)
                return Json(new { success = false, message = "Không thể đọc dữ liệu đặt bàn." });

            // Tạo link thanh toán (ví dụ PayOS hoặc trang QR riêng)
            var paymentLink = Url.Action("DepositPayment", "Payment",
                new { id = dto.ReservationId, amount = dto.DepositAmount }, Request.Scheme);

            // Nội dung SMS
            string message = $"[SapaFoRest] Quý khách vui lòng thanh toán đặt cọc {dto.DepositAmount:N0} VND để giữ bàn. " +
                             $"Thanh toán tại: {paymentLink}";

            try
            {
                using var smsClient = new HttpClient();
                smsClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(Encoding.ASCII.GetBytes("e1U7HuMJtajomx5s8higT05vxieVGyOt:")));

                var smsPayload = new
                {
                    to = new[] { reservation.CustomerPhone },
                    content = message,
                    type = 2,   // Gửi qua brandname
                    sender = "" // bắt buộc để trống nếu type=2
                };

                var json = JsonConvert.SerializeObject(smsPayload);
                var response = await smsClient.PostAsync("https://api.speedsms.vn/index.php/sms/send",
                    new StringContent(json, Encoding.UTF8, "application/json"));

                var smsResult = await response.Content.ReadAsStringAsync();

                Console.WriteLine("====================================");
                Console.WriteLine("[SpeedSMS Response] " + smsResult);
                Console.WriteLine("[Nội dung SMS gửi đi]");
                Console.WriteLine($"  ➜ Gửi tới: {reservation.CustomerPhone}");
                Console.WriteLine($"  ➜ Nội dung: {message}");
                Console.WriteLine("====================================");

                // Nếu gửi thành công
                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "✅ Đã gửi yêu cầu đặt cọc qua SMS cho khách hàng." });
                }

                // Nếu là lỗi "sender not found"
                if (smsResult.Contains("sender not found", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("[⚠️ DEMO MODE] Chưa đăng ký brandname - chỉ hiển thị nội dung SMS trong log.");
                   
                    return Json(new
                    {
                        success = true,
                        message = "⚠️ Chưa đăng ký brandname, SMS chỉ hiển thị DEMO MODE trong log (chưa gửi thật)."
                    });
                }

                // Lỗi khác
                Console.WriteLine("[❌ Lỗi khác khi gửi SMS]");
                return Json(new { success = false, message = "❌ Gửi SMS thất bại: " + smsResult });
            }
            catch (Exception ex)
            {
                Console.WriteLine("====================================");
                Console.WriteLine($"[SpeedSMS Exception] {ex.Message}");
                Console.WriteLine("[DEMO MODE] Tin nhắn gửi thử:");
                Console.WriteLine($"  ➜ Gửi tới: {reservation.CustomerPhone}");
                Console.WriteLine($"  ➜ Nội dung: {message}");
                Console.WriteLine("====================================");

                return Json(new
                {
                    success = true,
                    message = "⚠️ DEMO MODE (do lỗi API hoặc chưa có brandname). Xem nội dung SMS trong log."
                });
            }
        }

        public class DepositRequestDto
        {
            public int ReservationId { get; set; }
            public decimal DepositAmount { get; set; }
        }
       



    }
}
