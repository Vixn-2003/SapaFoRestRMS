using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using WebSapaForestForStaff.DTOs.AreaManage;


namespace WebSapaForestForStaff.Controllers
{
    public class AreaManageController : Controller
    {
        private readonly HttpClient _client;

        public AreaManageController(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7096/api/");
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchName, int? floor, int page = 1, int pageSize = 10)
        {
            var query = $"Area?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(searchName)) query += $"&searchName={searchName}";
            if (floor.HasValue) query += $"&floor={floor}";

            var response = await _client.GetAsync(query);
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không thể tải dữ liệu khu vực từ API.";
                return View(new List<AreaDto>());
            }

            var result = await response.Content.ReadFromJsonAsync<AreaResponse>();
            ViewBag.TotalCount = result?.Total ?? 0;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchName = searchName;
            ViewBag.Floor = floor;

            return View(result?.Data ?? new List<AreaDto>());
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(AreaCreateDto dto)
        {
            try
            {
                var response = await _client.PostAsJsonAsync("Area", dto);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Thêm khu vực thành công!";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Có lỗi xảy ra khi thêm khu vực: " + content;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi kết nối API: " + ex.Message;
            }

            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var response = await _client.GetAsync($"Area/{id}");
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không tìm thấy khu vực.";
                return RedirectToAction(nameof(Index));
            }

            var area = await response.Content.ReadFromJsonAsync<AreaDto>();

            var model = new AreaUpdateDto
            {
                AreaId = area.AreaId,
                AreaName = area.AreaName,
                Floor = area.Floor,
                Description = area.Description
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, AreaUpdateDto dto)
        {
            try
            {
                var response = await _client.PutAsJsonAsync($"Area/{id}", dto);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Cập nhật khu vực thành công!";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Có lỗi xảy ra khi cập nhật khu vực: " + content;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi kết nối API: " + ex.Message;
            }

            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _client.DeleteAsync($"Area/{id}");
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Xóa khu vực thành công!";
            else
                TempData["Error"] = "Có lỗi khi xóa khu vực: " + content;

            return RedirectToAction(nameof(Index));
        }

        // --- Lớp phụ dùng cho Deserialize ---
        private class AreaResponse
        {
            public List<AreaDto> Data { get; set; } = new();
            public int Total { get; set; }
        }
    }
}
