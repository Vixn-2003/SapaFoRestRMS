using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Json;
using WebSapaForestForStaff.DTOs.TableManage;
using WebSapaForestForStaff.Models;

namespace WebSapaForestForStaff.Controllers
{
    public class TableManageController : Controller
    {
        private readonly HttpClient _client;

        public TableManageController(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7096/api/");
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            int? capacity,
            int? areaId,
            string? status,
            int page = 1,
            int pageSize = 10)
        {
            // Query string
            var query = $"TableManager?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search)) query += $"&search={search}";
            if (capacity.HasValue) query += $"&capacity={capacity}";
            if (areaId.HasValue) query += $"&areaId={areaId}";
            if (!string.IsNullOrWhiteSpace(status)) query += $"&status={status}";

            var response = await _client.GetAsync(query);
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không thể tải dữ liệu từ API.";
                return View(new List<TableManageDto>());
            }

            var areaResponse = await _client.GetAsync("Area?page=1&pageSize=100");
            ViewBag.Areas = areaResponse.IsSuccessStatusCode
                ? (await areaResponse.Content.ReadFromJsonAsync<AreaApiResponse>())?.Data ?? new List<AreaDto>()
                : new List<AreaDto>();

            var result = await response.Content.ReadFromJsonAsync<TableResponse>();
            ViewBag.TotalCount = result?.TotalCount ?? 0;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;
            ViewBag.Capacity = capacity;
            ViewBag.AreaId = areaId;
            ViewBag.Status = status;

            return View(result?.Data ?? new List<TableManageDto>());
        }


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Lấy danh sách Area để hiển thị dropdown
            var areaResponse = await _client.GetAsync("Area?page=1&pageSize=100");
            ViewBag.Areas = areaResponse.IsSuccessStatusCode
                ? (await areaResponse.Content.ReadFromJsonAsync<AreaApiResponse>())?.Data ?? new List<AreaDto>()
                : new List<AreaDto>();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(TableCreateDto dto)
        {
            try
            {
                var response = await _client.PostAsJsonAsync("TableManager", dto);
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(response.StatusCode);
                Console.WriteLine(content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Thêm bàn thành công!";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Có lỗi xảy ra khi thêm bàn: " + content;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi kết nối API: " + ex.Message;
            }

            // Nếu thất bại, vẫn cần lấy Area để hiển thị dropdown
            var areaResponse = await _client.GetAsync("Area?page=1&pageSize=100");
            ViewBag.Areas = areaResponse.IsSuccessStatusCode
                ? (await areaResponse.Content.ReadFromJsonAsync<AreaApiResponse>())?.Data ?? new List<AreaDto>()
                : new List<AreaDto>();

            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var response = await _client.GetAsync($"TableManager/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var table = await response.Content.ReadFromJsonAsync<TableManageDto>();

            var model = new TableUpdateDto
            {
                TableNumber = table.TableNumber,
                Capacity = table.Capacity,
                Status = table.Status,
                AreaId = table.AreaId
            };

            var areaResponse = await _client.GetAsync("Area?page=1&pageSize=100");
            var areas = areaResponse.IsSuccessStatusCode
                ? (await areaResponse.Content.ReadFromJsonAsync<AreaApiResponse>())?.Data ?? new List<AreaDto>()
                : new List<AreaDto>();

            // Tạo SelectList và gán vào ViewBag
            ViewBag.Areas = new SelectList(areas, "AreaId", "AreaName", model.AreaId);
            ViewBag.StatusList = new SelectList(new List<string> { "Available", "Maintenance" }, model.Status);

            return View(model);
        }



        [HttpPost]
        public async Task<IActionResult> Edit(int id, TableUpdateDto dto)
        {
            try
            {
                var response = await _client.PutAsJsonAsync($"TableManager/{id}", dto);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Cập nhật bàn thành công!";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Có lỗi xảy ra khi cập nhật bàn: " + content;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi kết nối API: " + ex.Message;
            }

            // Lấy Area để hiển thị dropdown
            var areaResponse = await _client.GetAsync("Area?page=1&pageSize=100");
            ViewBag.Areas = areaResponse.IsSuccessStatusCode
                ? (await areaResponse.Content.ReadFromJsonAsync<AreaApiResponse>())?.Data ?? new List<AreaDto>()
                : new List<AreaDto>();

            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _client.DeleteAsync($"TableManager/{id}");
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Xóa bàn thành công!";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Có lỗi khi xóa bàn: {error}";
            }

            return RedirectToAction(nameof(Index));
        }



        // Internal classes for API deserialization
        private class TableResponse
        {
            public int TotalCount { get; set; }
            public List<TableManageDto> Data { get; set; } = new();
        }
        private class AreaApiResponse
        {
            public List<AreaDto> Data { get; set; } = new();
            public int Total { get; set; }
        }
    }
}
