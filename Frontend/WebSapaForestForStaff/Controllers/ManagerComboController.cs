using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebSapaForestForStaff.DTOs.ManagementCombo;

namespace WebSapaForestForStaff.Controllers
{
    public class ManagerComboController : Controller
    {

        private readonly HttpClient _httpClient;

        public ManagerComboController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient(); // Dùng DI chuẩn
            _httpClient.BaseAddress = new Uri("https://localhost:7096/api/");
        }
        // GET: ComboController
        public async Task<IActionResult> Index(
     string search = null,
     bool? isAvailable = null,
     int pageIndex = 1,
     int pageSize = 5,
     string period = "week"  // thêm param chọn thời gian thống kê
 )
        {
            var result = new PagedResult<ComboDisplayDto>(new List<ComboDisplayDto>(), 0, pageIndex, pageSize);

            try
            {
                // 1. Lấy danh sách combo theo filter + phân trang (giữ nguyên)
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
                    var apiResult = JsonConvert.DeserializeObject<PagedResult<ComboDisplayDto>>(json);
                    if (apiResult?.Items != null)
                        result = apiResult;
                }
                ViewData["Search"] = search;
                ViewData["SelectedStatus"] = isAvailable;
                // 2. Song song gọi API thống kê
                var topCombosTask = _httpClient.GetAsync($"ManagerCombo/api/combo/top?period={period}");
                var lowCombosTask = _httpClient.GetAsync($"ManagerCombo/api/combo/low?period={period}");
                var overviewTask = _httpClient.GetAsync("ManagerCombo/api/combo/overview");

                await Task.WhenAll(topCombosTask, lowCombosTask, overviewTask);

                // 3. Xử lý kết quả API thống kê
                if (topCombosTask.Result.IsSuccessStatusCode)
                {
                    var json = await topCombosTask.Result.Content.ReadAsStringAsync();
                    ViewBag.TopCombos = JsonConvert.DeserializeObject<List<ComboDisplayDto>>(json);
                }
                else
                {
                    ViewBag.TopCombos = new List<ComboDisplayDto>();
                }

                if (lowCombosTask.Result.IsSuccessStatusCode)
                {
                    var json = await lowCombosTask.Result.Content.ReadAsStringAsync();
                    ViewBag.LowCombos = JsonConvert.DeserializeObject<List<ComboDisplayDto>>(json);
                }
                else
                {
                    ViewBag.LowCombos = new List<ComboDisplayDto>();
                }

                if (overviewTask.Result.IsSuccessStatusCode)
                {
                    var json = await overviewTask.Result.Content.ReadAsStringAsync();
                    ViewBag.Overview = JsonConvert.DeserializeObject<dynamic>(json);
                }
                else
                {
                    ViewBag.Overview = new { TotalActiveCombos = 0, TotalOrdersWeek = 0, TotalOrdersMonth = 0 };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                ViewBag.TopCombos = new List<ComboDisplayDto>();
                ViewBag.LowCombos = new List<ComboDisplayDto>();
                ViewBag.Overview = new { TotalActiveCombos = 0, TotalOrdersWeek = 0, TotalOrdersMonth = 0 };
            }

            ViewBag.Period = period;

            // Truyền vào View cả PagedResult + các thống kê qua ViewBag
            return View("~/Views/Menu/ListCombo.cshtml", result);
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

            return PartialView("~/Views/Menu/_ComboListPartial.cshtml", comboResult);
        }




        // GET: ComboController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ComboController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ComboController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Combo/Edit/5
        [HttpGet("EditCombo/{id}")]
        public async Task<IActionResult> EditCombo(int id)
        {
            try
            {
                // Gọi API ManagerCombo
                var response = await _httpClient.GetAsync($"ManagerCombo/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound($"Combo với id={id} không tồn tại.");
                }

                // Đọc dữ liệu JSON trả về thành DTO
                var combo = await response.Content.ReadFromJsonAsync<ComboDetailDto>();

                if (combo == null)
                    return NotFound("Không thể đọc dữ liệu combo.");

                // Truyền combo sang view EditCombo.cshtml
                return View("EditCombo", combo); // 🎯 Tên view giữ nguyên
            }
            catch (Exception ex)
            {
                // Trả về lỗi chi tiết hơn để debug
                return StatusCode(500, $"Lỗi khi lấy combo: {ex.Message}");
            }
        }




        // POST: ComboController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

    }
}
