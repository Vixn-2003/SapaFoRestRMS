using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using WebSapaForestForStaff.Models;

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
        [HttpGet]
        public IActionResult CreateReservation()
        {
            return View(new ReservationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReservation(ReservationViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

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

            var response = await _client.PostAsync("Reservation", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Đặt bàn thành công!";
                return RedirectToAction("Index"); // trở về danh sách reservations
            }
            else
            {
                ModelState.AddModelError("", "Không thể đặt bàn. Vui lòng thử lại.");
                return View(model);
            }
        }

    }
}
