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

        public async Task<IActionResult> Index()
        {
            var response = await _client.GetAsync("ReservationStaff/reservations/pending-confirmed");
            if (!response.IsSuccessStatusCode) return View(new List<ReservationStaffViewModel>());

            var json = await response.Content.ReadAsStringAsync();
            var reservations = JsonConvert.DeserializeObject<List<ReservationStaffViewModel>>(json);
            return View(reservations);
        }

        public async Task<IActionResult> AssignTables(int id)
        {
            var resResponse = await _client.GetAsync("ReservationStaff/reservations/pending-confirmed");
            var resJson = await resResponse.Content.ReadAsStringAsync();
            var reservations = JsonConvert.DeserializeObject<List<ReservationStaffViewModel>>(resJson);
            var reservation = reservations.FirstOrDefault(r => r.ReservationId == id);
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
        public async Task<IActionResult> AssignTablesPost(int ReservationId, List<int> TableIds,
            bool RequireDeposit, decimal? DepositAmount)
        {
            var dto = new
            {
                ReservationId = ReservationId,
                TableIds = TableIds,
                RequireDeposit = RequireDeposit,
                DepositAmount = DepositAmount,
                StaffId = 3, // lấy Staff hiện tại từ session hoặc login
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

    }
}
