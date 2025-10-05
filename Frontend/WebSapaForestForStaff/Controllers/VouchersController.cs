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


    }


}
