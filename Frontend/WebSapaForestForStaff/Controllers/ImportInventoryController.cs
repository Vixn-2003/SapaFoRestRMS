using BusinessAccessLayer.DTOs.Inventory;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.DTOs.Inventory;
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
            // Gọi các API
            var response = await _httpClient.GetAsync("api/InventoryIngredient");
            var responseIdPurchase = await _httpClient.GetAsync("api/PurchaseOrder");
            var responseSupplier = await _httpClient.GetAsync("api/Supplier");
            var responseWarehouse = await _httpClient.GetAsync("api/Warehouse");
            var responseUnit = await _httpClient.GetAsync("api/Unit");

            // Kiểm tra phản hồi
            if (!response.IsSuccessStatusCode)
                return NotFound("Không tìm thấy nguyên liệu nào.");

            if (!responseSupplier.IsSuccessStatusCode)
                return NotFound("Không tìm thấy nhà cung cấp nào.");

            if (!responseWarehouse.IsSuccessStatusCode)
                return NotFound("Không tìm thấy danh sách kho nào.");

            if (!responseUnit.IsSuccessStatusCode)
                return NotFound("Không tìm thấy danh sách đơn vị tính nào.");

            // Đọc dữ liệu
            var json = await response.Content.ReadAsStringAsync();
            var jsonSupplier = await responseSupplier.Content.ReadAsStringAsync();
            var jsonIdPurchase = await responseIdPurchase.Content.ReadAsStringAsync();
            var jsonWarehouse = await responseWarehouse.Content.ReadAsStringAsync();
            var jsonUnit = await responseUnit.Content.ReadAsStringAsync();

            // Giải mã JSON
            var supplierList = JsonConvert.DeserializeObject<List<SupplierDTO>>(jsonSupplier);
            var purchaseList = JsonConvert.DeserializeObject<List<PurchaseOrderDTO>>(jsonIdPurchase);
            var ingredientList = JsonConvert.DeserializeObject<List<InventoryIngredientDTO>>(json);
            var warehouseList = JsonConvert.DeserializeObject<List<WarehouseDTO>>(jsonWarehouse);
            var unitList = JsonConvert.DeserializeObject<List<UnitDTO>>(jsonUnit);

            warehouseList = warehouseList?.Where(w => w.IsActive).ToList() ?? new List<WarehouseDTO>();

            // ✅ THÊM: MAP UNIT VÀO INGREDIENT
            if (ingredientList != null && unitList != null)
            {
                foreach (var ingredient in ingredientList)
                {
                    if (ingredient.UnitId.HasValue)
                    {
                        ingredient.Unit = unitList.FirstOrDefault(u => u.UnitId == ingredient.UnitId.Value)
                                          ?? new UnitDTO(); // Tạo Unit rỗng nếu không tìm thấy
                    }
                    else
                    {
                        ingredient.Unit = new UnitDTO(); // Tạo Unit rỗng nếu không có UnitId
                    }
                }
            }

            // Tạo model
            var importIngredient = new ImportIngredient
            {
                SupplierDTOs = supplierList,
                InventoryIngredientDTOs = ingredientList,
                WarehouseDTOs = warehouseList,
                PurchaseOrderDTOs = purchaseList,
                unitDTOs = unitList
            };

            return View("~/Views/Menu/ImportInventory.cshtml", importIngredient);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitImport([FromForm] ImportSubmitModel model)
        {
            if (model == null)
                return BadRequest("Thiếu dữ liệu đơn nhập.");

            try
            {
                // ✅ 1. VALIDATE VÀ PARSE DỮ LIỆU
                Console.WriteLine($"ImportList raw: {model.ImportList}");

                List<ImportItemModel>? importItems = null;

                if (string.IsNullOrWhiteSpace(model.ImportList))
                {
                    return BadRequest("Danh sách nguyên liệu trống.");
                }

                try
                {
                    importItems = JsonConvert.DeserializeObject<List<ImportItemModel>>(model.ImportList);
                    Console.WriteLine($"Parsed items count: {importItems?.Count ?? 0}");
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON Parse Error: {ex.Message}");
                    return BadRequest($"Lỗi parse JSON: {ex.Message}");
                }

                if (importItems == null || !importItems.Any())
                {
                    return BadRequest("Danh sách nguyên liệu trống hoặc không hợp lệ.");
                }

                // Validate dữ liệu cơ bản
                if (model.SupplierId == null)
                    return BadRequest("Thiếu thông tin nhà cung cấp.");

                if (model.ProofFile == null || model.ProofFile.Length == 0)
                    return BadRequest("Thiếu hình ảnh minh chứng.");

                // ✅ 2. TẠO MULTIPART FORM DATA ĐỂ GỬI SANG API BACKEND
                var formData = new MultipartFormDataContent();

                // Thêm các field thông tin cơ bản
                formData.Add(new StringContent(model.ImportCode), "ImportCode");
                formData.Add(new StringContent(model.ImportDate.ToString("o")), "ImportDate"); // ISO 8601 format
                formData.Add(new StringContent(model.SupplierId.ToString()), "SupplierId");
                formData.Add(new StringContent(model.CreatorId.ToString()), "CreatorId");
                //formData.Add(new StringContent(model.CheckId?.ToString() ?? ""), "CheckId");

                // ✅ Thêm danh sách items dưới dạng JSON string
                var itemsJson = JsonConvert.SerializeObject(importItems.Select(item => new
                {
                    IngredientId = item.IngredientId,
                    IngredientCode = item.Code,
                    IngredientName = item.Name,
                    Unit = item.Unit,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    WarehouseName = item.WarehouseName,
                    TotalPrice = item.Quantity * item.UnitPrice
                }));

                formData.Add(new StringContent(itemsJson, Encoding.UTF8, "application/json"), "Items");

                // ✅ Thêm FILE ẢNH
                if (model.ProofFile != null && model.ProofFile.Length > 0)
                {
                    var fileStream = model.ProofFile.OpenReadStream();
                    var streamContent = new StreamContent(fileStream);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(model.ProofFile.ContentType);
                    formData.Add(streamContent, "ProofFile", model.ProofFile.FileName);
                }

                Console.WriteLine("Sending data to API Backend...");

                // ✅ 3. GỬI SANG API BACKEND
                var response = await _httpClient.PostAsync("api/ImportIngredient/Create", formData);

                Console.WriteLine($"API Response Status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Success: {result}");
                    return Ok(new { success = true, message = "Đơn nhập hàng đã được tạo thành công!", data = result });
                }

                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Error: {error}");
                return StatusCode((int)response.StatusCode, new { success = false, message = error });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { success = false, message = $"Lỗi server: {ex.Message}" });
            }
        }
    }
}