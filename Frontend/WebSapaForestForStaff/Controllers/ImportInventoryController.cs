using BusinessAccessLayer.DTOs.Inventory;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.Models;

namespace WebSapaForestForStaff.Controllers
{
    public class ImportInventoryController : Controller
    {
        private readonly HttpClient _httpClient;

        public ImportInventoryController(HttpClient httpClient)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:7096/")
            };
        }


        public async Task<IActionResult> Index()
        {
            // Gọi API nguyên liệu
            var response = await _httpClient.GetAsync("api/InventoryIngredient");
            // Gọi API nhà cung cấp
            var responseSupplier = await _httpClient.GetAsync("api/Supplier");

            // Kiểm tra phản hồi
            if (!response.IsSuccessStatusCode)
            {
                return NotFound("Không tìm thấy nguyên liệu nào.");
            }
            else if (!responseSupplier.IsSuccessStatusCode)
            {
                return NotFound("Không tìm thấy nhà cung cấp nào.");
            }

            // Đọc dữ liệu đúng cho từng response
            var json = await response.Content.ReadAsStringAsync();
            var jsonSupplier = await responseSupplier.Content.ReadAsStringAsync();

            // Giải mã dữ liệu JSON
            var supplierList = JsonConvert.DeserializeObject<List<SupplierDTO>>(jsonSupplier);
            var ingredientList = JsonConvert.DeserializeObject<List<InventoryIngredientDTO>>(json);

            // Tạo model tổng hợp
            var importIngredient = new ImportIngredient
            {
                SupplierDTOs = supplierList,
                InventoryIngredientDTOs = ingredientList
            };

            return View("~/Views/Menu/ImportInventory.cshtml", importIngredient);
        }


        [HttpPost]
        public async Task<IActionResult> SubmitImport([FromForm] ImportSubmitModel model)
        {
            if (model == null)
                return BadRequest("Thiếu dữ liệu đơn nhập.");

            // Gửi toàn bộ form data (bao gồm file) sang API
            var formData = new MultipartFormDataContent();

            formData.Add(new StringContent(model.SupplierName ?? ""), "SupplierName");
            formData.Add(new StringContent(model.CreatorName ?? ""), "CreatorName");
            formData.Add(new StringContent(model.CreatorPhone ?? ""), "CreatorPhone");
            formData.Add(new StringContent(model.CheckerName ?? ""), "CheckerName");
            formData.Add(new StringContent(model.CheckerPhone ?? ""), "CheckerPhone");

            // Convert danh sách nguyên liệu sang JSON
            var jsonList = JsonConvert.SerializeObject(model.ImportList);
            formData.Add(new StringContent(jsonList, Encoding.UTF8, "application/json"), "ImportList");

            // ✅ Thêm file ảnh nếu có
            if (model.ProofFile != null && model.ProofFile.Length > 0)
            {
                var fileContent = new StreamContent(model.ProofFile.OpenReadStream());
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(model.ProofFile.ContentType);
                formData.Add(fileContent, "ProofFile", model.ProofFile.FileName);
            }

            // Gửi request sang API backend
            var response = await _httpClient.PostAsync("api/ImportInventory", formData);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return Ok(result);
            }

            var error = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, error);
        }


    }
}
