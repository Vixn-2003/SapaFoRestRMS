using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebSapaForestForStaff.DTOs;
using System.Net.Http.Json; // Cần thêm cái này

namespace WebSapaForestForStaff.Controllers
{
    public class DashboardTableController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public DashboardTableController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Tên Action nên là "Index" để dễ dàng map với View
        public async Task<IActionResult> Index(
      int? floor,
      string? areaName,
      string? status,
      string? searchString,
      int page = 1)
        {
            var httpClient = _httpClientFactory.CreateClient("BackendApi");
            int pageSize = 12; // Đặt số lượng bàn mỗi trang (giống API)
            // --- Xây dựng URL động ---
            var queryParams = new Dictionary<string, string>();
            if (floor.HasValue) queryParams.Add("floor", floor.Value.ToString());
            if (!string.IsNullOrEmpty(areaName)) queryParams.Add("areaName", areaName);
            if (!string.IsNullOrEmpty(status)) queryParams.Add("status", status);
            if (!string.IsNullOrEmpty(searchString)) queryParams.Add("searchString", searchString);
            queryParams.Add("page", page.ToString());
            queryParams.Add("pageSize", pageSize.ToString());
            var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={kv.Value}"));

            var apiUrl = $"https://localhost:7096/api/DashboardTable/List-Table?{queryString}";

            DashboardDataDto dashboardData = new DashboardDataDto();

            try
            {
                var response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    // --- SỬA LỖI HOA/THƯỜNG: VẪN GIỮ LẠI ---
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    dashboardData = await response.Content.ReadFromJsonAsync<DashboardDataDto>(options);
                }
                else
                {
                    // ĐẶT BREAKPOINT Ở ĐÂY để xem response.StatusCode là gì
                }
            }
            catch (Exception ex)
            {
                // ĐẶT BREAKPOINT Ở ĐÂY để xem lỗi ex.Message
            }

            ViewData["CurrentPage"] = page;
            ViewData["PageSize"] = pageSize;
            // Model.TotalCount đã tự có từ dashboardData rồi

            ViewData["CurrentFloor"] = floor;
            ViewData["CurrentArea"] = areaName;
            ViewData["CurrentStatus"] = status;
            // ĐẶT BREAKPOINT Ở ĐÂY để kiểm tra dashboardData.Tables.Count
            return View(dashboardData);
        }
    }
}