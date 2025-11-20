using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using WebSapaForestForStaff.DTOs;

namespace WebSapaForestForStaff.Controllers
{
    public class ShiftsController : Controller
    {
        private readonly HttpClient _http;

        public ShiftsController(IHttpClientFactory httpFactory)
        {
            _http = httpFactory.CreateClient();
        }

        // Trang chính phân ca
        public IActionResult Index()
        {
            return View();
        }

        // Lấy ca cho FullCalendar
        [HttpGet]
        public async Task<IActionResult> GetShifts()
        {
            var res = await _http.GetAsync("https://localhost:7096/api/Shifts");
            if (!res.IsSuccessStatusCode) return BadRequest();

            var json = await res.Content.ReadAsStringAsync();
            var shifts = JsonSerializer.Deserialize<List<ShiftDTO>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var events = shifts.Select(s => new
            {
                id = s.ShiftId,
                title = s.StaffName,
                start = s.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = s.EndTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                extendedProps = new
                {
                    staffId = s.StaffId,
                    shiftType = s.ShiftType
                }
            });


            return Json(events);
        }

        // Cập nhật ca khi kéo thả hoặc resize
        [HttpPut]
        public async Task<IActionResult> UpdateShift(int id, [FromBody] ShiftUpdateDTO dto)
        {
            var content = new StringContent(JsonSerializer.Serialize(dto), System.Text.Encoding.UTF8, "application/json");
            var res = await _http.PutAsync($"https://localhost:7096/api/Shifts/{id}", content);
            if (res.IsSuccessStatusCode) return Ok();
            return BadRequest();
        }
        [Route("Shifts/DeleteShift/{id}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteShift(int id)
        {
            var res = await _http.DeleteAsync($"https://localhost:7096/api/Shifts/{id}");
            if (res.IsSuccessStatusCode) return Ok();
            var body = await res.Content.ReadAsStringAsync();
            return BadRequest(body);
        }

        // Lấy danh sách nhân viên
        [HttpGet]
        public async Task<IActionResult> GetStaffs()
        {
            var res = await _http.GetAsync("https://localhost:7096/api/Staffs");
            if (!res.IsSuccessStatusCode) return BadRequest();

            var json = await res.Content.ReadAsStringAsync();
            var staffs = JsonSerializer.Deserialize<List<StaffDTO>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return Json(staffs);
        }

        // Lấy danh sách template ca
        [HttpGet]
        public async Task<IActionResult> GetTemplates()
        {
            var res = await _http.GetAsync("https://localhost:7096/api/ShiftTemplates");
            if (!res.IsSuccessStatusCode) return BadRequest();

            var json = await res.Content.ReadAsStringAsync();
            var templates = JsonSerializer.Deserialize<List<ShiftTemplateDTO>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return Json(templates);
        }

        // Tạo ca lặp tuần
        [HttpPost]
        public async Task<IActionResult> CreateRecurringShift([FromBody] WeeklyRecurringShiftCreateDTO dto)
        {
            var content = new StringContent(JsonSerializer.Serialize(dto), System.Text.Encoding.UTF8, "application/json");
            var res = await _http.PostAsync("https://localhost:7096/api/WeeklyRecurringShifts", content);
            if (res.IsSuccessStatusCode) return Ok();
            return BadRequest();
        }
        [HttpPost]
        public async Task<IActionResult> CreateShiftWithRepeat([FromBody] ShiftCreateWithRepeatDTO dto)
        {
            var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
            var res = await _http.PostAsync("https://localhost:7096/api/Shifts/CreateWithRepeat", content);

            if (res.IsSuccessStatusCode) return Ok();
            var body = await res.Content.ReadAsStringAsync();
            return BadRequest(body);
        }
        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            var res = await _http.GetAsync("https://localhost:7096/api/Departments");
            if (!res.IsSuccessStatusCode) return BadRequest();

            var json = await res.Content.ReadAsStringAsync();
            var deps = JsonSerializer.Deserialize<List<DepartmentDTO>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return Json(deps);
        }


    }
}
