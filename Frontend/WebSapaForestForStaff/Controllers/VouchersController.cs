using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Web;
using WebSapaForestForStaff.Models.VoucherDTO;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace WebSapaForestForStaff.Controllers
{
    public class VouchersController : Controller
    {
        private readonly HttpClient _httpClient;

        public VouchersController()
        {
            _httpClient = new HttpClient();
        }

        public async Task<IActionResult> Index(
     string keyword = "",
     string discountType = "",
     decimal? discountValue = null,
     DateTime? startDate = null,
     DateTime? endDate = null,
     decimal? minOrderValue = null,
     decimal? maxDiscount = null,
     int pageNumber = 1,
     int pageSize = 10)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);

            if (!string.IsNullOrEmpty(keyword)) query["keyword"] = keyword;
            if (!string.IsNullOrEmpty(discountType)) query["discountType"] = discountType;
            if (discountValue.HasValue) query["discountValue"] = discountValue.Value.ToString();
            if (startDate.HasValue) query["startDate"] = startDate.Value.ToString("yyyy-MM-dd");
            if (endDate.HasValue) query["endDate"] = endDate.Value.ToString("yyyy-MM-dd");
            if (minOrderValue.HasValue) query["minOrderValue"] = minOrderValue.Value.ToString();
            if (maxDiscount.HasValue) query["maxDiscount"] = maxDiscount.Value.ToString();

            query["pageNumber"] = pageNumber.ToString();
            query["pageSize"] = pageSize.ToString();

            string apiUrl = $"https://localhost:7096/api/Voucher?{query}";

            var response = await _httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
                return View("Error");

            var jsonString = await response.Content.ReadAsStringAsync();

            var pagedResult = JsonConvert.DeserializeObject<PagedResult<VoucherDto>>(jsonString);

            return View(pagedResult);
        }
        public async Task<IActionResult> Details(int id, int pageNumber = 1)
        {
            var apiUrl = $"https://localhost:7096/api/Voucher/{id}";
            try
            {
                var voucher = await _httpClient.GetFromJsonAsync<VoucherDto>(apiUrl);
                if (voucher == null)
                {
                    return NotFound();
                }
                ViewData["PageNumber"] = pageNumber;

                return View(voucher);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, "Error calling API: " + ex.Message);
            }
        }


        // GET: Vouchers/Edit/5
        public async Task<IActionResult> Edit(int id, int pageNumber = 1)
        {
            var apiUrl = $"https://localhost:7096/api/Voucher/{id}";
            try
            {
                var voucher = await _httpClient.GetFromJsonAsync<VoucherDto>(apiUrl);
                if (voucher == null)
                {
                    return NotFound();
                }
                ViewData["PageNumber"] = pageNumber;
                return View(voucher);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, "Error calling API: " + ex.Message);
            }
        }

        // POST: Vouchers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VoucherDto model, int pageNumber = 1)
        {
            if (!ModelState.IsValid)
            {
                ViewData["PageNumber"] = pageNumber;
                return View(model);
            }

            var apiUrl = $"https://localhost:7096/api/Voucher/{id}";

            // Lấy voucher gốc từ API để giữ VoucherId và Code
            var existingVoucher = await _httpClient.GetFromJsonAsync<VoucherDto>(apiUrl);
            if (existingVoucher == null)
            {
                return NotFound();
            }

            // Giữ nguyên VoucherId và Code
            model.VoucherId = existingVoucher.VoucherId;
            model.Code = existingVoucher.Code;

            // Gửi PUT request cập nhật
            var response = await _httpClient.PutAsJsonAsync(apiUrl, model);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index", new { pageNumber });
            }
            else
            {
                ModelState.AddModelError("", "Update failed: " + response.ReasonPhrase);
                ViewData["PageNumber"] = pageNumber;
                return View(model);
            }
        }

        //Delete
        [HttpGet]
        public async Task<IActionResult> Delete(int id, int pageNumber = 1)
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var apiUrl = $"https://localhost:7096/api/Voucher/{id}";

                    var response = await httpClient.DeleteAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = "Voucher deleted successfully.";
                        return RedirectToAction("Index", new { pageNumber });
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Delete failed. Server responded with status: {response.StatusCode}";
                        return RedirectToAction("Index", new { pageNumber });
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                    return RedirectToAction("Index", new { pageNumber });
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> IsDelete(int pageNumber = 1, int pageSize = 10)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["pageNumber"] = pageNumber.ToString();
            query["pageSize"] = pageSize.ToString();

            string apiUrl = $"https://localhost:7096/api/Voucher/deleted?{query}";

            var response = await _httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
                return View("Error");

            var jsonString = await response.Content.ReadAsStringAsync();

            var pagedResult = JsonConvert.DeserializeObject<PagedResult<VoucherDto>>(jsonString);

            // Truyền thông tin phân trang xuống view qua ViewBag
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;

            return View(pagedResult);
        }



        [HttpPost]
        public async Task<IActionResult> Restore(int id)
        {
            string apiUrl = $"https://localhost:7096/api/voucher/restore/{id}";

            // Tạo request HTTP PUT không có body
            var request = new HttpRequestMessage(HttpMethod.Put, apiUrl);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Khôi phục voucher thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Khôi phục voucher thất bại: " + response.ReasonPhrase;
            }

            return RedirectToAction("IsDelete");
        }


        //Create 
        // GET: Vouchers/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Vouchers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VoucherCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            string apiUrl = "https://localhost:7096/api/Voucher";

            var response = await _httpClient.PostAsJsonAsync(apiUrl, dto);

            if (response.IsSuccessStatusCode)
            {
                var createdVoucher = await response.Content.ReadFromJsonAsync<VoucherDto>();
                if (createdVoucher != null)
                {
                    return RedirectToAction("Details", new { id = createdVoucher.VoucherId });
                }
                else
                {
                    ModelState.AddModelError("", "Tạo voucher thất bại: Không nhận được dữ liệu voucher");
                    return View(dto);
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                try
                {
                    // Nếu là JSON và có field "message"
                    var errorObj = JsonConvert.DeserializeObject<dynamic>(errorContent);
                    string errorMessage = errorObj?.message?.ToString() ?? "Tạo voucher thất bại";

                    ModelState.AddModelError("", errorMessage);
                }
                catch
                {
                    // Nếu không phải JSON, hiển thị toàn bộ lỗi dạng chuỗi
                    ModelState.AddModelError("", "Tạo voucher thất bại: " + errorContent);
                }

                return View(dto);
            }
        }



    }


}
