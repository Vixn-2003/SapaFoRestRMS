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


        // Action này sẽ xử lý URL: /DashboardTable/ListOrder
        public async Task<IActionResult> ListOrder(
     string? searchTerm,
     string? status,
     DateTime? filterDate, 
     string? filterSlot,   
     int page = 1)
        {
            var httpClient = _httpClientFactory.CreateClient("BackendApi");
            int pageSize = 10;

            // --- Xây dựng URL động ---
            var queryParams = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(searchTerm)) queryParams.Add("searchTerm", searchTerm);

            // Logic Status
            if (!string.IsNullOrEmpty(status)) queryParams.Add("status", status);
            else queryParams.Add("status", "all");

            // ⭐️ THÊM THAM SỐ NGÀY & SLOT VÀO URL ⭐️
            if (filterDate.HasValue)
                queryParams.Add("reservationDate", filterDate.Value.ToString("yyyy-MM-dd")); 

            if (!string.IsNullOrEmpty(filterSlot))
                queryParams.Add("timeSlot", filterSlot); // Tên param phải khớp với API Backend

            queryParams.Add("pageNumber", page.ToString());
            queryParams.Add("pageSize", pageSize.ToString());

            var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            var apiUrl = $"https://localhost:7096/api/DashboardTable?{queryString}";

            var viewModel = new ReservationListViewModel();

            try
            {
                var response = await httpClient.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    viewModel.Reservations = await response.Content.ReadFromJsonAsync<List<ReservationListDto>>(options);

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
                    viewModel.Reservations = new List<ReservationListDto>();
                    TempData["ErrorMessage"] = "Không thể tải dữ liệu từ API.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi kết nối: {ex.Message}";
            }

            // Gửi lại View để fill vào input
            ViewData["CurrentSearch"] = searchTerm;
            ViewData["CurrentStatus"] = status ?? "all";
            ViewData["CurrentDate"] = filterDate?.ToString("yyyy-MM-dd"); // ✨ Để fill vào input date
            ViewData["CurrentSlot"] = filterSlot; // ✨ Để fill vào dropdown
            ViewData["CurrentPage"] = page;

            return View(viewModel);
        }

        // Action này sẽ nhận 'id' (chính là tableId) từ link ở Bước 1
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