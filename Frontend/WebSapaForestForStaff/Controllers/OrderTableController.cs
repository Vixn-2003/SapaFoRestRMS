using Microsoft.AspNetCore.Mvc;
using WebSapaFoRestForStaff.DTOs.OrderTable;
using System.Text; // <-- THÊM
using System.Net; // <-- THÊM
using Microsoft.AspNetCore.Mvc.Rendering; // <-- THÊM

namespace WebSapaFoRestForStaff.Controllers
{
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
    }
}