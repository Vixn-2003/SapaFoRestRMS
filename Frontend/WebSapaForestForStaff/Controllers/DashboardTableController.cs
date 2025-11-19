using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.DTOs.OrderGuest;
using WebSapaForestForStaff.DTOs.OrderGuest.ListOrder;

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

        // ⭐️⭐️ BẮT ĐẦU ACTION MỚI CHO DANH SÁCH ĐẶT BÀN ⭐️⭐️

        // Action này sẽ xử lý URL: /DashboardTable/ListOrder
        public async Task<IActionResult> ListOrder(
            string? searchTerm,
            string? status,
            int page = 1)
        {
            var httpClient = _httpClientFactory.CreateClient("BackendApi");
            int pageSize = 10; // Đặt pageSize cho trang này

            // --- Xây dựng URL động cho API Reservations ---
            var queryParams = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(searchTerm)) queryParams.Add("searchTerm", searchTerm);
            if (!string.IsNullOrEmpty(status)) queryParams.Add("status", status);
            else queryParams.Add("status", "all"); // Luôn gửi "all" nếu không chọn

            queryParams.Add("pageNumber", page.ToString());
            queryParams.Add("pageSize", pageSize.ToString());

            var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            var apiUrl = $"https://localhost:7096/api/DashboardTable?{queryString}"; // ⭐️ Dùng API Reservations

            var viewModel = new ReservationListViewModel();

            try
            {
                var response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    // Tùy chọn để đọc JSON (giống hệt code của bạn)
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    // 1. Đọc Body (chứa List<ReservationListDto>)
                    viewModel.Reservations = await response.Content.ReadFromJsonAsync<List<ReservationListDto>>(options);

                    // 2. Đọc Header "X-Pagination"
                    if (response.Headers.TryGetValues("X-Pagination", out var headerValues))
                    {
                        var paginationJson = headerValues.FirstOrDefault();
                        if (paginationJson != null)
                        {
                            viewModel.Pagination = JsonSerializer.Deserialize<PaginationInfo>(paginationJson, options);
                        }
                    }
                }
                else
                {
                    // Xử lý lỗi nếu API trả về 500, 404...
                    viewModel.Reservations = new List<ReservationListDto>();
                    // Bạn có thể thêm TempData
                    TempData["ErrorMessage"] = "Không thể tải dữ liệu từ API.";
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi (API không chạy, v.v.)
                TempData["ErrorMessage"] = $"Lỗi kết nối: {ex.Message}";
            }

            // Gửi các giá trị filter về View để giữ
            ViewData["CurrentSearch"] = searchTerm;
            ViewData["CurrentStatus"] = status ?? "all";
            ViewData["CurrentPage"] = page;

            return View(viewModel); // ⭐️ Trả về View() với Model
        }

        // Action này sẽ nhận 'id' (chính là tableId) từ link ở Bước 1
        // Sửa lại Action OrderDetail
        public async Task<IActionResult> OrderDetail(int id, int? categoryId, string? searchString)
        {
            var httpClient = _httpClientFactory.CreateClient("BackendApi");

            // 1. Xây dựng Query String (categoryId, searchString)
            var queryParams = new List<string>();

            if (categoryId.HasValue)
            {
                queryParams.Add($"categoryId={categoryId}");
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                // Lưu ý: Dùng Uri.EscapeDataString để xử lý ký tự đặc biệt (dấu cách, tiếng Việt)
                queryParams.Add($"searchString={Uri.EscapeDataString(searchString)}");
            }

            string queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";

            // 2. Ghép vào URL API (Lưu ý: Đảm bảo đúng đường dẫn API của bạn)
            // API của bạn có dạng: .../GetStaffOrder/{id}?categoryId=1&searchString=abc
            var apiUrl = $"https://localhost:7096/api/DashboardTable/MenuOrder/{id}{queryString}";

            StaffOrderScreenDto model = new StaffOrderScreenDto();

            try
            {
                var response = await httpClient.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    model = await response.Content.ReadFromJsonAsync<StaffOrderScreenDto>(options);
                }
                else
                {
                    ViewData["ErrorMessage"] = "Không thể tải chi tiết đơn hàng.";
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = $"Lỗi kết nối: {ex.Message}";
            }
            // === ⭐️ THÊM ĐOẠN NÀY: GỌI API LẤY DANH MỤC ===
            try
            {
                var catResponse = await httpClient.GetAsync("https://localhost:7096/api/DashboardTable/categories");
                if (catResponse.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var categories = await catResponse.Content.ReadFromJsonAsync<List<CategoryDto>>(options);

                    // Lưu vào ViewData để View dùng
                    ViewData["Categories"] = categories;
                }
            }
            catch
            {
                // Nếu lỗi thì gán list rỗng để không crash trang
                ViewData["Categories"] = new List<CategoryDto>();
            }
            // 3. Lưu lại trạng thái tìm kiếm để hiển thị lại trên View
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentCategory"] = categoryId;
            ViewData["TableId"] = id; // Lưu lại ID bàn để dùng cho Form Action

            // Mẹo: Để hiển thị danh sách các nút Category (Tất cả, Đồ ăn, Đồ uống...), 
            // bạn nên gọi thêm 1 API lấy danh sách Category ở đây và gán vào ViewBag.
            // Ví dụ: ViewBag.Categories = await _categoryService.GetAllAsync();

            return View(model);
        }
    }
}