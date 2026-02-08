
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.DTOs.Department;
using WebSapaForestForStaff.DTOs.Shift;
using WebSapaForestForStaff.DTOs.ShiftTemplateDTOs;

namespace WebSapaForestForStaff.Controllers
{
    public class ShiftsController : Controller
    {
        private readonly HttpClient _http;

        public ShiftsController(IHttpClientFactory factory)
        {
            _http = factory.CreateClient();
            _http.BaseAddress = new Uri("https://localhost:7096/");
        }

        // Hiển thị tuần
        public async Task<IActionResult> Index(DateTime? week)
        {
            DateTime startOfWeek = week ?? DateTime.Today;
            while (startOfWeek.DayOfWeek != DayOfWeek.Monday)
                startOfWeek = startOfWeek.AddDays(-1);

            var shifts = await _http.GetFromJsonAsync<List<ShiftViewDTO>>("api/shift");

            var data = shifts!
                .Where(s => s.Date.Date >= startOfWeek.Date &&
                            s.Date.Date <= startOfWeek.AddDays(6).Date)
                .ToList();
            // Lấy danh sách department
            var departments = await _http.GetFromJsonAsync<List<DepartmentDTO>>("api/Departments");
            ViewBag.Departments = departments;
            // Lấy danh sách template từ API ShiftTemplate
            var templates = await _http.GetFromJsonAsync<List<ShiftTemplateResponseDTO>>("api/ShiftTemplate");
            ViewBag.Templates = templates;
            ViewBag.WeekStart = startOfWeek;
            return View(data);
        }

        // POST: Tạo ca
        [HttpPost]
        public async Task<IActionResult> Create(CreateShiftDTO dto)
        {
            var res = await _http.PostAsJsonAsync("api/shift", dto);
            return RedirectToAction("Index");
        }

        // PUT: Sửa ca
        [HttpPost]
        public async Task<IActionResult> Update(int id, UpdateShiftDTO dto)
        {
            var res = await _http.PutAsJsonAsync($"api/shift/{id}", dto);
            return RedirectToAction("Index");
        }

        // DELETE: Xóa ca
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var res = await _http.DeleteAsync($"api/shift/{id}");
            return RedirectToAction("Index");
        }
    }
}
