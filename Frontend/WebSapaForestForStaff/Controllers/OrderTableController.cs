using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // <-- THÊM
using System.Net; // <-- THÊM
using System.Text; // <-- THÊM
using System.Text.Json;
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.DTOs.OrderAssitance;
using WebSapaFoRestForStaff.DTOs.OrderTable;

namespace WebSapaFoRestForStaff.Controllers
{
    [Authorize(Policy = "Position:WaiterOrCashier")]

    public class OrderTableController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _apiBaseUrl;

        public OrderTableController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;

            var apiConfig = configuration.GetSection("ApiSettings");
            // Sửa logic này để lấy đúng BaseUrl (ví dụ: http://192.168.1.47:5180)
            _apiBaseUrl = apiConfig.GetValue<string>("BaseUrl").Replace("/api", "");
        }
        // === THAY THẾ TOÀN BỘ HÀM INDEX CŨ BẰNG HÀM NÀY ===
        public async Task<IActionResult> Index(string? searchString, string? areaName, int? floor, int page = 1)
        {
            int pageSize = 10;
            var httpClient = _httpClientFactory.CreateClient("API");

            // 1. Build Query String cho API
            var queryBuilder = new StringBuilder();
            // Khớp với endpoint MỚI của bạn
            queryBuilder.Append($"api/OrderTable/tables?page={page}&pageSize={pageSize}");

            if (!string.IsNullOrEmpty(searchString))
                queryBuilder.Append($"&searchString={WebUtility.UrlEncode(searchString)}");
            if (!string.IsNullOrEmpty(areaName))
                queryBuilder.Append($"&areaName={WebUtility.UrlEncode(areaName)}");
            if (floor.HasValue)
                queryBuilder.Append($"&floor={floor.Value}");

            // 2. Chuẩn bị ViewModel để gửi sang .cshtml
            var viewModel = new PagedQrResultViewModel
            {
                Page = page,
                PageSize = pageSize,
                SearchString = searchString,
                AreaName = areaName,
                Floor = floor
            };

            try
            {
                // 3. Gọi API lấy danh sách bàn (đã lọc/phân trang)
                var response = await httpClient.GetAsync(queryBuilder.ToString());
                if (response.IsSuccessStatusCode)
                {
                    // 4. Đọc JSON response (dùng class ApiPagedTableResponse)
                    var apiResult = await response.Content.ReadFromJsonAsync<ApiPagedTableResponse>();

                    viewModel.Tables = apiResult.Data; // <-- Lấy từ "Data"
                    viewModel.TotalCount = apiResult.TotalCount;
                    viewModel.TotalPages = (int)Math.Ceiling(apiResult.TotalCount / (double)pageSize);
                }
                else
                {
                    ViewBag.Error = $"Lỗi khi gọi API: {response.StatusCode}";
                }

                // 5. Gọi API lấy các tùy chọn cho bộ lọc
                // (Giả định bạn có 2 endpoint này trong API Controller)
                var areaNames = await httpClient.GetFromJsonAsync<List<string>>("api/OrderTable/Filters/AreaNames");
                var floors = await httpClient.GetFromJsonAsync<List<int?>>("api/OrderTable/Filters/Floors");

                viewModel.AreaNames = new SelectList(areaNames, viewModel.AreaName);
                viewModel.Floors = new SelectList(floors, viewModel.Floor);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Không thể kết nối đến API: {ex.Message}";
                // Khởi tạo rỗng để View không bị crash
                viewModel.AreaNames = new SelectList(new List<string>());
                viewModel.Floors = new SelectList(new List<int?>());
            }

            // 6. Gửi BaseUrl (đã lấy từ config) sang View
            ViewBag.ApiBaseUrl = _apiBaseUrl;

            // 7. Trả về View với ViewModel mới (thay vì List cũ)
            return View(viewModel);
        }

        // === THAY THẾ TOÀN BỘ HÀM INDEX CŨ BẰNG HÀM NÀY ===
        public async Task<IActionResult> AssistanceList(string? sort, int page = 1)
        {
            var httpClient = _httpClientFactory.CreateClient("BackendApi");
            int pageSize = 10;

            // --- Build query string cho API Pending ---
            var queryParams = new Dictionary<string, string>
            {
                ["page"] = page.ToString(),
                ["pageSize"] = pageSize.ToString()
            };

            if (!string.IsNullOrEmpty(sort))
                queryParams.Add("sort", sort); // gửi sort = "newest" hoặc "oldest"

            var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={kv.Value}"));

            var apiUrl = $"{_apiBaseUrl}/api/OrderTable/Pending?{queryString}";

            // --- Chuẩn bị model mặc định ---
            var resultModel = new WebSapaForestForStaff.DTOs.OrderAssitance.PagedResult<AssistanceResponseDto>
            {
                Page = page,
                PageSize = pageSize,
                Items = new List<AssistanceResponseDto>()
            };

            try
            {
                var response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    resultModel = await response.Content.ReadFromJsonAsync<
                        WebSapaForestForStaff.DTOs.OrderAssitance.PagedResult<AssistanceResponseDto>>(options);
                }
                else
                {
                    TempData["ErrorMessage"] = $"Không tải được dữ liệu từ API. StatusCode: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi kết nối: " + ex.Message;
            }

            return View(resultModel);
        }



    }
}