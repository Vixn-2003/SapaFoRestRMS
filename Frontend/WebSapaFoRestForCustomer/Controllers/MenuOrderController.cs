using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using WebSapaFoRestForCustomer.DTOs.OrderTable;
using WebSapaFoRestForCustomer.Models;

namespace WebSapaFoRestForCustomer.Controllers
{
    public class MenuOrderController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration; // Thêm dòng này
        private readonly string _apiBaseUrl; //  Thêm dòng này

        public MenuOrderController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration; 

            _apiBaseUrl = _configuration.GetValue<string>("ApiSettings:BaseUrl").Replace("/api", "");
        }

        [HttpGet]
        public async Task<IActionResult> Index(
        int tableId,
        int? categoryId,     
        string? searchString) 
        {
            var httpClient = _httpClientFactory.CreateClient("API");

            // 1. Build query string cho API
            var queryBuilder = new StringBuilder();
            queryBuilder.Append($"api/OrderTable/MenuOrder/{tableId}?");
            if (categoryId.HasValue && categoryId > 0)
            {
                queryBuilder.Append($"categoryId={categoryId}&");
            }
            if (!string.IsNullOrEmpty(searchString))
            {
                queryBuilder.Append($"searchString={searchString}");
            }
            string apiEndpoint = queryBuilder.ToString();

            try
            {
                // 2. Gọi API lấy Menu (đã lọc)
                var response = await httpClient.GetAsync(apiEndpoint);

                if (response.IsSuccessStatusCode)
                {
                    var viewModel = await response.Content.ReadFromJsonAsync<MenuPageViewModel>();

                    // Nhóm menu theo CategoryName (như cũ)
                    var menuGrouped = viewModel.MenuItems
                        .Where(m => m.IsAvailable)
                        .GroupBy(m => m.CategoryName)
                        .OrderBy(g => g.Key);

                    // Gửi dữ liệu sang View (như cũ)
                    ViewBag.TableId = tableId;
                    ViewBag.ApiBaseUrl = _apiBaseUrl;
                    ViewBag.OrderedItems = viewModel.OrderedItems;
                    ViewBag.TableNumber = viewModel.TableNumber;
                    ViewBag.AreaName = viewModel.AreaName;
                    ViewBag.Floor = viewModel.Floor;

                    // 3. (MỚI) Gọi API lấy danh sách Danh mục
                    try
                    {
                        var categories = await httpClient.GetFromJsonAsync<List<MenuCategoryViewModel>>("api/OrderTable/MenuCategories");
                        ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName", categoryId); // (categoryId là selected value)
                    }
                    catch
                    {
                        ViewBag.Categories = new SelectList(new List<MenuCategoryViewModel>()); // Lỗi thì trả về list rỗng
                    }

                    // 4. (MỚI) Gửi giá trị lọc hiện tại
                    ViewBag.CurrentSearchString = searchString;

                    return View(menuGrouped);
                }
                else // API trả về lỗi
                {
                    string errorMsg = $"Lỗi không xác định từ API ({response.StatusCode})."; // Default message
                    try
                    {
                        // Cố gắng đọc JSON {"message": "..."}
                        var errorResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                        if (errorResponse != null && errorResponse.TryGetValue("message", out var messageValue))
                        {
                            // === CHỈ LẤY NỘI DUNG MESSAGE ===
                            errorMsg = messageValue;
                        }
                        else
                        {
                            // Nếu không có key "message", đọc cả nội dung text
                            errorMsg = await response.Content.ReadAsStringAsync();
                        }
                    }
                    catch (System.Text.Json.JsonException) // Nếu không phải JSON hợp lệ
                    {
                        // Đọc lỗi dạng text thường
                        errorMsg = await response.Content.ReadAsStringAsync();
                    }
                    catch (Exception readEx) // Các lỗi đọc khác
                    {
                        errorMsg = $"Lỗi khi đọc phản hồi từ API: {readEx.Message}";
                    }


                    ViewBag.Error = errorMsg; // Gán lỗi đã được cắt

                    // Tạo và truyền ErrorViewModel
                    return View("ErrorPage", new ErrorViewModel
                    {
                        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                    });
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Không thể kết nối đến API: {ex.Message}";
                return View("Error");
            }
        }
    }
}