using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json; // Dùng cho ReadFromJsonAsync
using WebSapaForestForStaff.DTOs.ManagementCombo;

namespace WebSapaForestForStaff.Controllers
{
    public class ManagerComboController : Controller
    {
        private readonly HttpClient _httpClient;

        public ManagerComboController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7096/api/");
        }
        public class ApiErrorResponse
        {
            public string errorCode { get; set; } = string.Empty;
            public string message { get; set; } = string.Empty;
        }

        // ==========================================================
        // 1. HÀM HỖ TRỢ (HELPERS)
        // ==========================================================

        // Helper: Load dữ liệu menu và top món (Dùng cho trang Edit)
        // Mục đích: Để khi load form hoặc khi validate lỗi, danh sách món ăn không bị mất
        private async Task LoadComboAuxData()
        {
            try
            {
                // A. Lấy tất cả menu (để hiện trong modal chọn món)
                var allMenuResponse = await _httpClient.GetAsync("ManagerCombo/AllMenu?pageSize=100");
                if (allMenuResponse.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = await allMenuResponse.Content.ReadFromJsonAsync<PagedResult<MenuItemDto>>(options);
                    ViewBag.AllDishes = result ?? new PagedResult<MenuItemDto>();
                }
                else
                {
                    ViewBag.AllDishes = new PagedResult<MenuItemDto>();
                }

                // B. Lấy Top món (Gợi ý)
                var topResponse = await _httpClient.GetAsync("ManagerCombo/Top_Item_new");
                if (topResponse.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var topMenu = await topResponse.Content.ReadFromJsonAsync<List<MenuItemDto>>(options);
                    ViewBag.TopDishes = topMenu ?? new List<MenuItemDto>();
                }
                else
                {
                    ViewBag.TopDishes = new List<MenuItemDto>();
                }
            }
            catch
            {
                // Tránh crash trang nếu API phụ trợ lỗi
                ViewBag.AllDishes = new PagedResult<MenuItemDto>();
                ViewBag.TopDishes = new List<MenuItemDto>();
            }
        }


        // GET: Combo list
        public async Task<IActionResult> Index(
            string search = null,
            bool? isAvailable = null,
            int pageIndex = 1,
            int pageSize = 5,
            string period = "week")
        {
            var result = new PagedResult<ComboDisplayDto>(new List<ComboDisplayDto>(), 0, pageIndex, pageSize);

            try
            {
                // 1. Lấy danh sách combo phân trang
                var query = new List<string>();
                if (!string.IsNullOrEmpty(search)) query.Add($"search={Uri.EscapeDataString(search)}");
                if (isAvailable.HasValue) query.Add($"isAvailable={isAvailable.Value}");
                query.Add($"pageIndex={pageIndex}");
                query.Add($"pageSize={pageSize}");

                var url = "ManagerCombo/GetListCombo?" + string.Join("&", query);

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject<PagedResult<ComboDisplayDto>>(json) ?? result;
                }

                // 2. Gọi API thống kê song song (Top, Low, Overview)
                var topTask = _httpClient.GetAsync($"ManagerCombo/api/combo/top?period={period}");
                var lowTask = _httpClient.GetAsync($"ManagerCombo/api/combo/low?period={period}");
                var overviewTask = _httpClient.GetAsync("ManagerCombo/api/combo/overview");

                await Task.WhenAll(topTask, lowTask, overviewTask);

                ViewBag.TopCombos = topTask.Result.IsSuccessStatusCode
                    ? JsonConvert.DeserializeObject<List<ComboDisplayDto>>(await topTask.Result.Content.ReadAsStringAsync())
                    : new List<ComboDisplayDto>();

                ViewBag.LowCombos = lowTask.Result.IsSuccessStatusCode
                    ? JsonConvert.DeserializeObject<List<ComboDisplayDto>>(await lowTask.Result.Content.ReadAsStringAsync())
                    : new List<ComboDisplayDto>();

                ViewBag.Overview = overviewTask.Result.IsSuccessStatusCode
                    ? JsonConvert.DeserializeObject<dynamic>(await overviewTask.Result.Content.ReadAsStringAsync())
                    : new { TotalActiveCombos = 0, TotalOrdersWeek = 0, TotalOrdersMonth = 0 };
            }
            catch
            {
                ViewBag.TopCombos = new List<ComboDisplayDto>();
                ViewBag.LowCombos = new List<ComboDisplayDto>();
                ViewBag.Overview = new { TotalActiveCombos = 0, TotalOrdersWeek = 0, TotalOrdersMonth = 0 };
            }

            ViewBag.Period = period;
            ViewData["Search"] = search;
            ViewData["SelectedStatus"] = isAvailable;

            return View("Index", result);
        }
        [HttpGet]
        public async Task<IActionResult> LoadComboList(string search = null, bool? isAvailable = null, int pageIndex = 1, int pageSize = 5)
        {
            // Gọi API như trong Index
            var query = new List<string>();
            if (!string.IsNullOrEmpty(search)) query.Add($"search={Uri.EscapeDataString(search)}");
            if (isAvailable.HasValue) query.Add($"isAvailable={isAvailable.Value}");
            query.Add($"pageIndex={pageIndex}");
            query.Add($"pageSize={pageSize}");

            var url = "ManagerCombo/GetListCombo?" + string.Join("&", query);
            var response = await _httpClient.GetAsync(url);

            PagedResult<ComboDisplayDto> comboResult = new PagedResult<ComboDisplayDto>(
                new List<ComboDisplayDto>(), 0, pageIndex, pageSize);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                comboResult = JsonConvert.DeserializeObject<PagedResult<ComboDisplayDto>>(json) ?? comboResult;
            }

            return PartialView("~/Views/ManagerCombo/_ComboListPartial.cshtml", comboResult);
        }

        // ==========================================================
        // 3. CHỈNH SỬA COMBO (EDIT - GET & POST)
        // ==========================================================

        // GET: Hiển thị form Edit
        [HttpGet("EditCombo/{id}")]
        public async Task<IActionResult> EditCombo(int id)
        {
            try
            {
                // 1. Lấy thông tin Combo chi tiết
                var comboResponse = await _httpClient.GetAsync($"ManagerCombo/{id}");
                if (!comboResponse.IsSuccessStatusCode) return NotFound();

                var comboData = await comboResponse.Content.ReadFromJsonAsync<ComboEditDto>();

                // 2. Load dữ liệu phụ trợ (Menu, Top dishes)
                // QUAN TRỌNG: Gọi hàm này để có dữ liệu đổ vào modal chọn món
                await LoadComboAuxData();

                // Trả về view Edit (Lưu ý tên file View phải là Edit.cshtml)
                return View("EditCombo", comboData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi Server: " + ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                using var formData = new MultipartFormDataContent();

                // ✅ Lấy dữ liệu từ Request.Form
                var comboId = Convert.ToInt32(Request.Form["ComboId"]);
                var name = Request.Form["Name"].ToString();
                var description = Request.Form["Description"].ToString();
                var sellingPrice = Convert.ToDecimal(Request.Form["SellingPrice"]);

                // ✅ Xử lý checkbox: nếu không có trong form = false, có = true
                bool isAvailable = false;
                if (Request.Form.ContainsKey("IsAvailable"))
                {
                    var checkboxValue = Request.Form["IsAvailable"].ToString();
                    // Checkbox gửi "true" khi checked, không gửi gì khi unchecked
                    isAvailable = checkboxValue.Contains("true", StringComparison.OrdinalIgnoreCase);
                }

                var imageUrl = Request.Form["ImageUrl"].ToString();

                // ✅ DEBUG
                System.Diagnostics.Debug.WriteLine($"=== EDIT COMBO DEBUG ===");
                System.Diagnostics.Debug.WriteLine($"ComboId: {comboId}");
                System.Diagnostics.Debug.WriteLine($"Name: {name}");
                System.Diagnostics.Debug.WriteLine($"IsAvailable: {isAvailable}");
                System.Diagnostics.Debug.WriteLine($"IsAvailable raw: {Request.Form["IsAvailable"]}");
                System.Diagnostics.Debug.WriteLine($"Files count: {Request.Form.Files.Count}");

                // ✅ Thêm các field vào FormData
                formData.Add(new StringContent(comboId.ToString()), "ComboId");
                formData.Add(new StringContent(name ?? ""), "Name");
                formData.Add(new StringContent(description ?? ""), "Description");
                formData.Add(new StringContent(sellingPrice.ToString()), "SellingPrice");
                formData.Add(new StringContent(isAvailable.ToString().ToLower()), "IsAvailable");

                // ✅ Gửi ImageUrl nếu không có file mới
                if (Request.Form.Files.Count == 0 && !string.IsNullOrEmpty(imageUrl))
                {
                    formData.Add(new StringContent(imageUrl), "ImageUrl");
                }

                // ✅ Xử lý file ảnh
                var imageFile = Request.Form.Files["ImageFile"];
                if (imageFile != null && imageFile.Length > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Image: {imageFile.FileName} ({imageFile.Length} bytes)");

                    var fileContent = new StreamContent(imageFile.OpenReadStream());
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType);
                    formData.Add(fileContent, "imageFile", imageFile.FileName);
                }

                // ✅ Thêm Items
                int index = 0;
                while (Request.Form.ContainsKey($"Items[{index}].MenuItemId"))
                {
                    var itemId = Request.Form[$"Items[{index}].MenuItemId"].ToString();
                    var quantity = Request.Form[$"Items[{index}].Quantity"].ToString();

                    formData.Add(new StringContent(itemId), $"Items[{index}].MenuItemId");
                    formData.Add(new StringContent(quantity), $"Items[{index}].Quantity");

                    index++;
                }

                // ✅ Gọi API
                var response = await _httpClient.PutAsync($"ManagerCombo/{id}", formData);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "✅ Cập nhật thành công!";
                    return RedirectToAction("Index");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"❌ API Error: {errorContent}");

                try
                {
                    var errorObj = JsonConvert.DeserializeObject<ApiErrorResponse>(errorContent);
                    ModelState.AddModelError("", errorObj?.message ?? "Lỗi không xác định");
                }
                catch
                {
                    ModelState.AddModelError("", errorContent);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Exception: {ex.Message}");
                ModelState.AddModelError("", $"❌ Lỗi: {ex.Message}");
            }

            // ✅ Load lại form khi lỗi
            var model = new ComboEditDto
            {
                ComboId = id,
                Name = Request.Form["Name"],
                Description = Request.Form["Description"],
                SellingPrice = decimal.TryParse(Request.Form["SellingPrice"], out var sp) ? sp : 0,
                IsAvailable = Request.Form["IsAvailable"].ToString().Contains("true", StringComparison.OrdinalIgnoreCase),
                ImageUrl = Request.Form["ImageUrl"]
            };

            await LoadComboAuxData();
            return View("EditCombo", model);
        }

        // ==========================================================
        // 2. TẠO MỚI COMBO (CREATE - GET & POST)
        // ==========================================================

        // GET: Hiển thị form Create
        [HttpGet("Create")] // Đường dẫn sẽ là /ManagerCombo/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                // 1. Load dữ liệu phụ trợ (Menu, Top dishes) để người dùng chọn món
                await LoadComboAuxData();

                // 2. Khởi tạo model rỗng để tránh lỗi null khi view render danh sách Items
                var emptyModel = new CreateComboDto
                {
                    Items = new List<ComboItemInput>(),
                    IsAvailable = true 
                };

                return View("Create", emptyModel);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi Server: " + ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Creates()
        {
            try
            {
                using var formData = new MultipartFormDataContent();

                // ✅ Lấy dữ liệu từ Request.Form
                var name = Request.Form["Name"].ToString();
                var description = Request.Form["Description"].ToString();
                var sellingPrice = Convert.ToDecimal(Request.Form["SellingPrice"]);

                // ✅ Xử lý checkbox IsAvailable
                bool isAvailable = false;
                if (Request.Form.ContainsKey("IsAvailable"))
                {
                    var checkboxValue = Request.Form["IsAvailable"].ToString();
                    isAvailable = checkboxValue.Contains("true", StringComparison.OrdinalIgnoreCase);
                }

                // ✅ DEBUG
                System.Diagnostics.Debug.WriteLine($"=== CREATE COMBO DEBUG ===");
                System.Diagnostics.Debug.WriteLine($"Name: {name}");
                System.Diagnostics.Debug.WriteLine($"SellingPrice: {sellingPrice}");
                System.Diagnostics.Debug.WriteLine($"IsAvailable: {isAvailable}");

                // ✅ Validate
                if (string.IsNullOrEmpty(name))
                {
                    ModelState.AddModelError("", "Tên combo không được để trống");
                    await LoadComboAuxData();
                    return View("Create", new CreateComboDto());
                }

                // ✅ Thêm các field cơ bản vào FormData
                formData.Add(new StringContent(name), "Name");
                formData.Add(new StringContent(description ?? ""), "Description");
                formData.Add(new StringContent(sellingPrice.ToString()), "SellingPrice");
                formData.Add(new StringContent(isAvailable.ToString().ToLower()), "IsAvailable");

                // ✅ Xử lý file ảnh
                var imageFile = Request.Form.Files["ImageFile"];
                if (imageFile != null && imageFile.Length > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"📸 Image: {imageFile.FileName} ({imageFile.Length} bytes)");

                    var fileContent = new StreamContent(imageFile.OpenReadStream());
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType);
                    formData.Add(fileContent, "ImageFile", imageFile.FileName);
                }

                // ✅ XỬ LÝ ITEMS - GỬI THEO DẠNG FORM-DATA ARRAY
                int index = 0;
                int itemCount = 0;

                // Đếm trước xem có bao nhiêu items
                while (Request.Form.ContainsKey($"Items[{index}].MenuItemId"))
                {
                    var itemId = Request.Form[$"Items[{index}].MenuItemId"].ToString();
                    var quantity = Request.Form[$"Items[{index}].Quantity"].ToString();

                    // ✅ QUAN TRỌNG: Thêm vào formData theo đúng format ASP.NET Model Binding
                    formData.Add(new StringContent(itemId), $"Items[{itemCount}].MenuItemId");
                    formData.Add(new StringContent(quantity), $"Items[{itemCount}].Quantity");

                    System.Diagnostics.Debug.WriteLine($"✅ Item {itemCount}: MenuItemId={itemId}, Quantity={quantity}");

                    index++;
                    itemCount++;
                }

                // ✅ Nếu không tìm thấy items, in ra debug
                if (itemCount == 0)
                {
                    System.Diagnostics.Debug.WriteLine("❌ KHÔNG TÌM THẤY ITEMS!");
                    System.Diagnostics.Debug.WriteLine("📋 Các keys trong Request.Form:");
                    foreach (var key in Request.Form.Keys)
                    {
                        System.Diagnostics.Debug.WriteLine($"   - {key} = {Request.Form[key]}");
                    }

                    ModelState.AddModelError("", "Combo phải có ít nhất 1 món ăn");
                    await LoadComboAuxData();
                    return View("Create", new CreateComboDto());
                }

                System.Diagnostics.Debug.WriteLine($"✅ Tổng số items: {itemCount}");

                // ✅ Gọi API
                var response = await _httpClient.PostAsync("ManagerCombo/CreateCombo", formData);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "✅ Tạo combo mới thành công!";
                    return RedirectToAction("Index");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"❌ API Error: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Error content: {errorContent}");

                try
                {
                    var errorObj = JsonConvert.DeserializeObject<ApiErrorResponse>(errorContent);
                    ModelState.AddModelError("", errorObj?.message ?? "Lỗi không xác định");
                }
                catch
                {
                    ModelState.AddModelError("", errorContent);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                ModelState.AddModelError("", $"❌ Lỗi: {ex.Message}");
            }

            // ✅ Nếu lỗi, load lại form
            await LoadComboAuxData();

            var model = new CreateComboDto
            {
                Name = Request.Form["Name"],
                Description = Request.Form["Description"],
                SellingPrice = decimal.TryParse(Request.Form["SellingPrice"], out var sp) ? sp : 0,
                IsAvailable = Request.Form["IsAvailable"].ToString().Contains("true", StringComparison.OrdinalIgnoreCase)
            };

            return View("Create", model);
        }
    }
}