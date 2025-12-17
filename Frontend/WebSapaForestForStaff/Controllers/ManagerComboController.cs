using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
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

        // POST: Xử lý lưu Edit

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ComboEditDto model)
        {
            // ===== VALIDATE =====
            if (model.Items == null || !model.Items.Any())
                ModelState.AddModelError("", "Combo phải có ít nhất 1 món.");

            if (model.Items.Any(i => i.Quantity < 2))
                ModelState.AddModelError("", "Mỗi món phải có số lượng ≥ 2.");

            if (!ModelState.IsValid)
            {
                await LoadComboAuxData();
                return View("EditCombo", model);
            }

            // ===== CALL API PUT =====
            var json = JsonConvert.SerializeObject(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"ManagerCombo/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Cập nhật thành công!";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Cập nhật thất bại.");
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

        // POST: Xử lý lưu Create
        // POST: Create Combo
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateComboDto model)
        {
            if (!ModelState.IsValid)
            {
                await LoadComboAuxData();
                return View("Create", model);
            }

            try
            {
                var json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("ManagerCombo/CreateCombo", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Tạo combo mới thành công!";
                    return RedirectToAction("Index");
                }

                // 🔥 Lấy lỗi từ BE
                var error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", error);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Không thể kết nối tới server.");
            }

            await LoadComboAuxData();
            return View("Create", model);
        }


    }
}