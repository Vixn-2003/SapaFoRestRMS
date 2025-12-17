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
            //  KHỞI TẠO CÁC DANH SÁCH RỖNG MẶC ĐỊNH
            var supplierList = new List<SupplierDTO>();
            var purchaseList = new List<PurchaseOrderDTO>();
            var ingredientList = new List<InventoryIngredientDTO>();
            var warehouseList = new List<WarehouseDTO>();
            var unitList = new List<UnitDTO>();

            try
            {
                //  GỌI API VÀ XỬ LÝ TỪNG ENDPOINT RIÊNG BIỆT

                // 1. Ingredients
                try
                {
                    var response = await _httpClient.GetAsync("api/InventoryIngredient");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        ingredientList = JsonConvert.DeserializeObject<List<InventoryIngredientDTO>>(json)
                                         ?? new List<InventoryIngredientDTO>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading ingredients: {ex.Message}");
                }

                // 2. Purchase Orders
                try
                {
                    var responseIdPurchase = await _httpClient.GetAsync("api/PurchaseOrder");
                    if (responseIdPurchase.IsSuccessStatusCode)
                    {
                        var jsonIdPurchase = await responseIdPurchase.Content.ReadAsStringAsync();
                        purchaseList = JsonConvert.DeserializeObject<List<PurchaseOrderDTO>>(jsonIdPurchase)
                                       ?? new List<PurchaseOrderDTO>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading purchase orders: {ex.Message}");
                }

                // 3. Suppliers
                try
                {
                    var responseSupplier = await _httpClient.GetAsync("/api/inventory/Supplier");
                    if (responseSupplier.IsSuccessStatusCode)
                    {
                        var jsonSupplier = await responseSupplier.Content.ReadAsStringAsync();
                        supplierList = JsonConvert.DeserializeObject<List<SupplierDTO>>(jsonSupplier)
                                       ?? new List<SupplierDTO>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading suppliers: {ex.Message}");
                }

                // 4. Warehouses
                try
                {
                    var responseWarehouse = await _httpClient.GetAsync("api/Warehouse");
                    if (responseWarehouse.IsSuccessStatusCode)
                    {
                        var jsonWarehouse = await responseWarehouse.Content.ReadAsStringAsync();
                        warehouseList = JsonConvert.DeserializeObject<List<WarehouseDTO>>(jsonWarehouse)
                                        ?? new List<WarehouseDTO>();

                        // Chỉ lấy kho active
                        warehouseList = warehouseList.Where(w => w.IsActive).ToList();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading warehouses: {ex.Message}");
                }

                // 5. Units
                try
                {
                    var responseUnit = await _httpClient.GetAsync("api/Unit");
                    if (responseUnit.IsSuccessStatusCode)
                    {
                        var jsonUnit = await responseUnit.Content.ReadAsStringAsync();
                        unitList = JsonConvert.DeserializeObject<List<UnitDTO>>(jsonUnit)
                                   ?? new List<UnitDTO>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading units: {ex.Message}");
                }

                //  MAP UNIT VÀO INGREDIENT (CHỈ NẾU CẢ 2 LIST ĐỀU CÓ DỮ LIỆU)
                if (ingredientList.Any() && unitList.Any())
                {
                    foreach (var ingredient in ingredientList)
                    {
                        if (ingredient.UnitId.HasValue)
                        {
                            ingredient.Unit = unitList.FirstOrDefault(u => u.UnitId == ingredient.UnitId.Value)
                                              ?? new UnitDTO();
                        }
                        else
                        {
                            ingredient.Unit = new UnitDTO();
                        }
                    }
                }

                //  TẠO DANH SÁCH NHÀ CUNG CẤP GẦN ĐÂY (AN TOÀN VỚI NULL)
                var recentSuppliers = new List<SupplierDTO>();
                if (purchaseList.Any() && supplierList.Any())
                {
                    recentSuppliers = purchaseList
                        .Where(p => p.Status == "Completed" && p.TimeConfirm.HasValue)
                        .OrderByDescending(p => p.TimeConfirm)
                        .GroupBy(p => p.SupplierId)
                        .Select(g => g.First())
                        .Take(5)
                        .Select(p => supplierList.FirstOrDefault(s => s.SupplierId == p.SupplierId))
                        .Where(s => s != null)
                        .ToList();
                }

                //  TẠO DANH SÁCH NGUYÊN LIỆU KHẨN CẤP (AN TOÀN VỚI NULL)
                var urgentIngredients = new List<InventoryIngredientDTO>();
                if (ingredientList.Any())
                {
                    urgentIngredients = ingredientList
                        .Where(i =>
                            i.TotalQuantity == 0 ||
                            i.IsLowStock ||
                            i.IsBelowReorderLevel ||
                            i.NeedUrgentRestock)
                        .OrderBy(i => i.TotalQuantity == 0 ? 0 :
                                     i.IsLowStock ? 1 :
                                     i.NeedUrgentRestock ? 2 : 3)
                        .ThenBy(i => i.TotalQuantity)
                        .Take(10)
                        .ToList();
                }

                //  TẠO MODEL VỚI TẤT CẢ DANH SÁCH (RỖNG HOẶC CÓ DỮ LIỆU)
                var importIngredient = new ImportIngredient
                {
                    SupplierDTOs = supplierList,
                    InventoryIngredientDTOs = ingredientList,
                    WarehouseDTOs = warehouseList,
                    PurchaseOrderDTOs = purchaseList,
                    unitDTOs = unitList,
                    RecentSupplierDTOs = recentSuppliers,
                    UrgentIngredientDTOs = urgentIngredients
                };

                //  THÊM THÔNG BÁO NẾU CÓ DANH SÁCH RỖNG
                if (!supplierList.Any())
                {
                    TempData["WarningMessage"] = "Không có nhà cung cấp nào trong hệ thống";
                }
                if (!ingredientList.Any())
                {
                    TempData["WarningMessage"] = (TempData["WarningMessage"]?.ToString() ?? "")
                        + (string.IsNullOrEmpty(TempData["WarningMessage"]?.ToString()) ? "" : ". ")
                        + "Không có nguyên liệu nào trong kho";
                }
                if (!warehouseList.Any())
                {
                    TempData["WarningMessage"] = (TempData["WarningMessage"]?.ToString() ?? "")
                        + (string.IsNullOrEmpty(TempData["WarningMessage"]?.ToString()) ? "" : ". ")
                        + "Không có kho nào đang hoạt động";
                }

                return View("~/Views/Menu/ImportInventory.cshtml", importIngredient);
            }
            catch (Exception ex)
            {
                //  XỬ LÝ LỖI TỔNG THỂ - VẪN TRẢ VỀ VIEW VỚI DANH SÁCH RỖNG
                Console.WriteLine($"Error in Index: {ex.Message}");

                var importIngredient = new ImportIngredient
                {
                    SupplierDTOs = supplierList,
                    InventoryIngredientDTOs = ingredientList,
                    WarehouseDTOs = warehouseList,
                    PurchaseOrderDTOs = purchaseList,
                    unitDTOs = unitList,
                    RecentSupplierDTOs = new List<SupplierDTO>(),
                    UrgentIngredientDTOs = new List<InventoryIngredientDTO>()
                };

                TempData["ErrorMessage"] = "Có lỗi khi tải dữ liệu. Vui lòng thử lại sau.";
                return View("~/Views/Menu/ImportInventory.cshtml", importIngredient);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitImport([FromForm] ImportSubmitModel model)
        {
            if (model == null)
                return BadRequest("Thiếu dữ liệu đơn nhập.");

            try
            {
                //  1. VALIDATE VÀ PARSE DỮ LIỆU
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

                //  2. TẠO MULTIPART FORM DATA ĐỂ GỬI SANG API BACKEND
                var formData = new MultipartFormDataContent();

                // Thêm các field thông tin cơ bản
                formData.Add(new StringContent(model.ImportCode), "ImportCode");
                formData.Add(new StringContent(model.ImportDate.ToString("o")), "ImportDate");
                formData.Add(new StringContent(model.SupplierId.ToString()), "SupplierId");
                formData.Add(new StringContent(model.CreatorId.ToString()), "CreatorId");

                //  Thêm danh sách items dưới dạng JSON string
                var itemsJson = JsonConvert.SerializeObject(importItems.Select(item => new
                {
                    IngredientId = item.IngredientId,
                    IngredientCode = item.Code,
                    IngredientName = item.Name,
                    Unit = item.Unit,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    WarehouseName = item.WarehouseName,
                    ExpiryDate = item.ExpiryDate,
                    TotalPrice = item.Quantity * item.UnitPrice
                }));

                formData.Add(new StringContent(itemsJson, Encoding.UTF8, "application/json"), "Items");

                //  Thêm FILE ẢNH
                if (model.ProofFile != null && model.ProofFile.Length > 0)
                {
                    var fileStream = model.ProofFile.OpenReadStream();
                    var streamContent = new StreamContent(fileStream);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(model.ProofFile.ContentType);
                    formData.Add(streamContent, "ProofFile", model.ProofFile.FileName);
                }

                Console.WriteLine("Sending data to API Backend...");

                //  3. GỬI SANG API BACKEND
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

        [HttpGet]
        public async Task<IActionResult> GetSupplierComparison(int ingredientId, string compareBy = "price")
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/PurchaseOrderDetail/CompareSuppliers/{ingredientId}?compareBy={compareBy}");

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound("Không tìm thấy lịch sử giao dịch");
                }

                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy dữ liệu so sánh", error = ex.Message });
            }
        }
    }
}