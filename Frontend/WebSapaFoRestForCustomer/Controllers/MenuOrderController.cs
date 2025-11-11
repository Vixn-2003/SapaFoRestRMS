using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Diagnostics;
// Thêm các DTOs của bạn ở đây
using WebSapaFoRestForCustomer.DTOs.OrderTable;
using WebSapaFoRestForCustomer.Models; // (Nếu bạn có ErrorViewModel)

public class MenuOrderController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly string _apiBaseUrl;

    public MenuOrderController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        // Đảm bảo "ApiSettings:BaseUrl" trong appsettings.json là đúng
        _apiBaseUrl = _configuration.GetValue<string>("ApiSettings:BaseUrl").Replace("/api", "");
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int tableId,
        int? categoryId, // Giờ sẽ nhận cả 0, -1
        string? searchString)
    {
        var httpClient = _httpClientFactory.CreateClient("API");

        // 1. Build query string (ĐÃ SỬA)
        var queryBuilder = new StringBuilder();
        queryBuilder.Append($"api/OrderTable/MenuOrder/{tableId}?");

        // === ĐÃ SỬA: Chấp nhận 0 (Tất cả) và -1 (Combos) ===
        if (categoryId.HasValue)
        {
            queryBuilder.Append($"categoryId={categoryId.Value}&");
        }
        // ===============================================

        if (!string.IsNullOrEmpty(searchString))
        {
            queryBuilder.Append($"searchString={searchString}");
        }
        string apiEndpoint = queryBuilder.ToString().TrimEnd('&'); // Xóa dấu & thừa

        try
        {
            // 2. Gọi API lấy Menu (đã lọc)
            var response = await httpClient.GetAsync(apiEndpoint);

            if (response.IsSuccessStatusCode)
            {
                // Đọc viewModel
                var viewModel = await response.Content.ReadFromJsonAsync<MenuPageViewModel>();

                // Gửi dữ liệu cơ bản sang View qua ViewBag
                ViewBag.TableId = tableId;
                ViewBag.ApiBaseUrl = _apiBaseUrl;
                ViewBag.OrderedItems = viewModel.OrderedItems; // Dùng cho JS
                ViewBag.TableNumber = viewModel.TableNumber;
                ViewBag.AreaName = viewModel.AreaName;
                ViewBag.Floor = viewModel.Floor;

                // 3. Lấy danh sách Danh mục (ĐÃ SỬA: Thêm try-catch rõ ràng)
                try
                {
                    var categories = await httpClient.GetFromJsonAsync<List<MenuCategoryViewModel>>("api/OrderTable/MenuCategories");

                    ViewBag.Categories = categories; // Gửi thẳng List
                    ViewBag.CurrentCategoryId = categoryId; // Báo cho View biết tab nào đang active
                }
                catch (Exception ex)
                {
                    // Lỗi này xảy ra nếu MenuCategoryViewModel.cs (MVC) không khớp với MenuCategoryDto.cs (API)
                    ViewBag.Categories = new List<MenuCategoryViewModel>(); // Lỗi thì trả về list rỗng

                    // Ném lỗi này ra màn hình vàng để bạn biết LỖI THỰC SỰ
                    throw new Exception($"Lỗi khi deserialize MenuCategories: {ex.Message}. Vui lòng kiểm tra lại file MenuCategoryViewModel.cs.", ex);
                }

                // 4. Gửi giá trị lọc (Giữ nguyên)
                ViewBag.CurrentSearchString = searchString;

                // 5. (ĐÃ SỬA) Gửi TOÀN BỘ viewModel sang View
                // File Index.cshtml của bạn phải có @model MenuPageViewModel
                return View(viewModel);
            }
            else // API /MenuOrder/ trả về lỗi (ví dụ: "Bàn không có khách")
            {
                string errorMsg = $"Lỗi không xác định từ API ({response.StatusCode}).";
                try
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    if (errorResponse != null && errorResponse.TryGetValue("message", out var messageValue))
                    {
                        errorMsg = messageValue;
                    }
                    else
                    {
                        errorMsg = await response.Content.ReadAsStringAsync();
                    }
                }
                catch { /* Bỏ qua nếu đọc lỗi thất bại */ }

                ViewBag.Error = errorMsg;
                return View("ErrorPage", new ErrorViewModel // Đảm bảo bạn có View "ErrorPage"
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                });
            }
        }
        catch (Exception ex) // Lỗi kết nối API
        {
            ViewBag.Error = $"Không thể kết nối đến API: {ex.Message}";
            return View("ErrorPage", new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}