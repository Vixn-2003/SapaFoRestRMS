using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.Models;
using System.Net.Http.Json;

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

        [HttpPost]
        public async Task<IActionResult> SaveTable(TableManageDto dto)
        {
            try
            {
                if (dto.TableId == 0)
                {
                    // Add
                    var response = await _client.PostAsJsonAsync("TableManager", dto);
                    if (response.IsSuccessStatusCode)
                        return Json(new { success = true, message = "Thêm bàn thành công" });
                }
                else
                {
                    // Edit
                    var response = await _client.PutAsJsonAsync($"TableManager/{dto.TableId}", dto);
                    if (response.IsSuccessStatusCode)
                        return Json(new { success = true, message = "Cập nhật bàn thành công" });
                }
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _client.DeleteAsync($"TableManager/{id}");
            if (response.IsSuccessStatusCode)
                return Json(new { success = true, message = "Xóa bàn thành công" });

            return Json(new { success = false, message = "Xóa thất bại" });
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
