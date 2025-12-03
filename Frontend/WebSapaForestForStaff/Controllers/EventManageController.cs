using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using WebSapaForestForStaff.DTOs; // Tạo DTO tương tự API
using Microsoft.AspNetCore.Http;

namespace WebSapaForestForStaff.Controllers
{
    public class EventManageController : Controller
    {
        private readonly HttpClient _client;

        public EventManageController(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7096/api/");
        }

        // GET: /EventManage
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
        {
            var response = await _client.GetAsync($"Events?search={search}&page={page}&pageSize={pageSize}");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không thể tải dữ liệu từ API.";
                return View(new List<EventDto>());
            }

            var result = await response.Content.ReadFromJsonAsync<ApiEventListResponse>();
            ViewBag.TotalCount = result?.TotalCount ?? 0;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;

            return View(result?.Data ?? new List<EventDto>());
        }

        // GET: /EventManage/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /EventManage/Create
        [HttpPost]
        public async Task<IActionResult> Create(EventCreateDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var content = new MultipartFormDataContent();
            content.Add(new StringContent(dto.Title), "Title");
            content.Add(new StringContent(dto.Description ?? ""), "Description");
            content.Add(new StringContent(dto.Location ?? ""), "Location");
            content.Add(new StringContent(dto.StartDate?.ToString("yyyy-MM-dd") ?? ""), "StartDate");
            content.Add(new StringContent(dto.EndDate?.ToString("yyyy-MM-dd") ?? ""), "EndDate");

            if (dto.Image != null)
            {
                var streamContent = new StreamContent(dto.Image.OpenReadStream());
                content.Add(streamContent, "Image", dto.Image.FileName);
            }

            var response = await _client.PostAsync("Events", content);
            if (response.IsSuccessStatusCode) return RedirectToAction(nameof(Index));

            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", error);
            return View(dto);
        }

        // GET: /EventManage/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var ev = await _client.GetFromJsonAsync<EventDto>($"Events/{id}");
            if (ev == null) return NotFound();

            var model = new EventUpdateDto
            {
                Title = ev.Title,
                Description = ev.Description,
                Location = ev.Location,
                StartDate = ev.StartDate,
                EndDate = ev.EndDate
            };

            ViewBag.Id = id;
            return View(model);
        }

        // POST: /EventManage/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(int id, EventUpdateDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var content = new MultipartFormDataContent();
            content.Add(new StringContent(dto.Title), "Title");
            content.Add(new StringContent(dto.Description ?? ""), "Description");
            content.Add(new StringContent(dto.Location ?? ""), "Location");
            content.Add(new StringContent(dto.StartDate?.ToString("yyyy-MM-dd") ?? ""), "StartDate");
            content.Add(new StringContent(dto.EndDate?.ToString("yyyy-MM-dd") ?? ""), "EndDate");

            if (dto.Image != null)
            {
                var streamContent = new StreamContent(dto.Image.OpenReadStream());
                content.Add(streamContent, "Image", dto.Image.FileName);
            }

            var response = await _client.PutAsync($"Events/{id}", content);
            if (response.IsSuccessStatusCode) return RedirectToAction(nameof(Index));

            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", error);
            return View(dto);
        }

        // POST: /EventManage/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _client.DeleteAsync($"Events/{id}");
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Xóa không thành công!";
            }
            else
            {
                TempData["Success"] = "Xóa thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        // Internal class để deserialize API list
        private class ApiEventListResponse
        {
            public int TotalCount { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
            public List<EventDto> Data { get; set; } = new();
        }
    }
}
