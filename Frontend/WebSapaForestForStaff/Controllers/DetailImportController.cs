using BusinessAccessLayer.DTOs.Inventory;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using WebSapaForestForStaff.DTOs;

namespace WebSapaForestForStaff.Controllers
{
    [Route("[controller]")]
    public class DetailImportController : Controller
    {
        private readonly HttpClient _httpClient;

        public DetailImportController(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://localhost:7096/");
        }

        [HttpGet("Detail/{id}")]
        public async Task<IActionResult> Detail(string id)
        {
            var response = await _httpClient.GetAsync($"api/PurchaseOrder/Detail/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return NotFound($"Không tìm thấy đơn hàng {id}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var order = JsonConvert.DeserializeObject<PurchaseOrderDTO>(json);

            return View("~/Views/Menu/DetailImport.cshtml", order);
        }


        [HttpPost("Confirm")]
        public async Task<IActionResult> Confirm([FromForm] ConfirmImportRequest model)
        {

            model.CheckId = 3;

            if (string.IsNullOrEmpty(model.PurchaseOrderId))
                return BadRequest("Thiếu mã đơn nhập.");

            if (string.IsNullOrEmpty(model.Status))
                return BadRequest("Thiếu trạng thái đơn hàng.");

            if (model.Status == "Cancelled" && string.IsNullOrWhiteSpace(model.RejectReason))
                return BadRequest("Vui lòng nhập lý do từ chối.");

            try
            {
                model.TimeConfirm = DateTime.Now;

                var response = await _httpClient.PostAsJsonAsync("api/PurchaseOrder/Confirm", model);

                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    var errorObj = JsonConvert.DeserializeObject<dynamic>(errorJson);

                    TempData["ErrorMessage"] = errorObj?.message?.ToString() ?? "Không thể xử lý đơn hàng.";
                    TempData["ShowModal"] = "error"; // ✅ THÊM DÒNG NÀY
                    return RedirectToAction("Detail", new { id = model.PurchaseOrderId });
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                var resultObj = JsonConvert.DeserializeObject<dynamic>(resultJson);

                if (model.Status == "Completed")
                {
                    TempData["SuccessMessage"] = "Đã xác nhận đơn nhập hàng thành công! Tất cả nguyên liệu đã được thêm vào kho.";
                    TempData["ShowModal"] = "success"; // ✅ THÊM DÒNG NÀY
                }
                else if (model.Status == "Cancelled")
                {
                    TempData["SuccessMessage"] = "Đã từ chối đơn nhập hàng thành công!";
                    TempData["ShowModal"] = "success"; // ✅ THÊM DÒNG NÀY
                }

                return RedirectToAction("Detail", new { id = model.PurchaseOrderId }); // ✅ SỬA DÒNG NÀY - Quay lại Detail thay vì Index
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi kết nối: " + ex.Message;
                TempData["ShowModal"] = "error"; // ✅ THÊM DÒNG NÀY
                return RedirectToAction("Detail", new { id = model.PurchaseOrderId });
            }
        }

        [HttpGet("Done/{id}")]
        public async Task<IActionResult> Done(string id)
        {
            var response = await _httpClient.GetAsync($"api/PurchaseOrder/Detail/{id}");
            if (!response.IsSuccessStatusCode)
            {
                return NotFound($"Không tìm thấy đơn hàng {id}");
            }
            var json = await response.Content.ReadAsStringAsync();
            var order = JsonConvert.DeserializeObject<PurchaseOrderDTO>(json);

            // Kiểm tra trạng thái đơn hàng
            if (order.Status != "Completed" && order.Status != "Cancelled")
            {
                TempData["ErrorMessage"] = "Đơn hàng này chưa được xử lý!";
                return RedirectToAction("Index", "MainImportInventory");
            }

            return View("~/Views/Menu/DoneImport.cshtml", order);
        }

        public class ConfirmImportRequest
        {
            public string PurchaseOrderId { get; set; } = null!;
            public int CheckId { get; set; }
            public DateTime TimeConfirm { get; set; }   // ❗ Thêm mới
            public string Status { get; set; } = null!;  // ✅ THÊM: "Completed" hoặc "Cancelled"
            public string? RejectReason { get; set; }    // ✅ THÊM: Lý do từ chối (nullable)
        }

    }
}
