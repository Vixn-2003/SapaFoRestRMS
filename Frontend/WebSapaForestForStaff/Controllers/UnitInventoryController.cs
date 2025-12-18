using BusinessAccessLayer.DTOs.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebSapaForestForStaff.DTOs.Inventory;

namespace WebSapaForestForStaff.Controllers
{
    [Authorize(Policy = "Position:Inventory")]
    public class UnitInventoryController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UnitInventoryController> _logger;

        public UnitInventoryController(IHttpClientFactory httpClientFactory, ILogger<UnitInventoryController> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7096/");
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new UnitWarehouseViewModel
            {
                Units = new List<UnitDTO>(),
                Warehouses = new List<WarehouseDTO>()
            };

            try
            {
                // ✅ Call API Unit
                var unitResponse = await _httpClient.GetAsync("api/Unit");
                if (unitResponse.IsSuccessStatusCode)
                {
                    var unitContent = await unitResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("Unit API Response: {Content}", unitContent);

                    viewModel.Units = JsonSerializer.Deserialize<List<UnitDTO>>(
                        unitContent,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }
                    ) ?? new List<UnitDTO>();

                    // ✅ KHÔNG CẦN THÊM GÌ CHO UNIT vì IngredientCount đã có sẵn từ API
                }

                // ✅ Call API Warehouse (giữ nguyên như code hiện tại)
                var warehouseResponse = await _httpClient.GetAsync("api/Warehouse");
                if (warehouseResponse.IsSuccessStatusCode)
                {
                    var warehouseContent = await warehouseResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("Warehouse API Response: {Content}", warehouseContent);

                    viewModel.Warehouses = JsonSerializer.Deserialize<List<WarehouseDTO>>(
                        warehouseContent,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }
                    ) ?? new List<WarehouseDTO>();

                    // ✅ Load số lượng lô cho từng kho
                    foreach (var warehouse in viewModel.Warehouses)
                    {
                        try
                        {
                            var batchResponse = await _httpClient.GetAsync($"api/Warehouse/warehouse/{warehouse.WarehouseId}");
                            if (batchResponse.IsSuccessStatusCode)
                            {
                                var batchContent = await batchResponse.Content.ReadAsStringAsync();
                                var batches = JsonSerializer.Deserialize<List<BatchDTO>>(
                                    batchContent,
                                    new JsonSerializerOptions
                                    {
                                        PropertyNameCaseInsensitive = true
                                    }
                                );
                                warehouse.BatchCount = batches?.Count ?? 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error loading batch count for warehouse {WarehouseId}", warehouse.WarehouseId);
                            warehouse.BatchCount = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading unit and warehouse data");
                ViewBag.Error = $"Lỗi: {ex.Message}";
            }

            return View("~/Views/Inventory/UnitInventory.cshtml", viewModel);
        }

        // ============ ADD UNIT ============
        [HttpPost]
        public async Task<IActionResult> AddUnit([FromBody] AddUnitRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UnitName))
                {
                    return Json(new { success = false, message = "Tên đơn vị không được để trống" });
                }

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        unitName = request.UnitName,
                        unitType = request.UnitType
                    }),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync("api/Unit", jsonContent);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var unit = JsonSerializer.Deserialize<UnitDTO>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return Json(new
                    {
                        success = true,
                        message = "Thêm đơn vị thành công",
                        unit = unit
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Lỗi từ API: {content}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding unit");
                return Json(new
                {
                    success = false,
                    message = "Lỗi: " + ex.Message
                });
            }
        }

        // ============ ADD WAREHOUSE ============

        [HttpPost]
        public async Task<IActionResult> AddWarehouse([FromBody] AddWarehouseRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Json(new { success = false, message = "Tên kho không được để trống" });
                }

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        name = request.Name,
                        isActive = true  // ✅ Mặc định true
                    }),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync("api/Warehouse", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    // ✅ Không cần parse response, chỉ cần báo thành công
                    return Json(new
                    {
                        success = true,
                        message = "Thêm kho thành công"
                    });
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError("API Error: {StatusCode} - {Content}", response.StatusCode, content);

                    return Json(new
                    {
                        success = false,
                        message = "Không thể thêm kho. Vui lòng thử lại."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding warehouse");
                return Json(new
                {
                    success = false,
                    message = "Lỗi: " + ex.Message
                });
            }
        }

        // ============ UPDATE WAREHOUSE ============
        [HttpPost]
        public async Task<IActionResult> UpdateWarehouse([FromBody] UpdateWarehouseRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Json(new { success = false, message = "Tên kho không được để trống" });
                }

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        warehouseId = request.WarehouseId,
                        name = request.Name,
                        isActive = request.IsActive
                    }),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PutAsync($"api/Warehouse/{request.WarehouseId}", jsonContent);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Cập nhật kho thành công"
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Tên kho đã tồn tại, không thể cập nhật"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật kho");
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // ============ DELETE WAREHOUSE ============
        [HttpPost]
        public async Task<IActionResult> DeleteWarehouse([FromBody] DeleteWarehouseRequest request)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/Warehouse/{request.WarehouseId}");

                if (response.IsSuccessStatusCode)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Xóa kho thành công"
                    });
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError("API Error: {StatusCode} - {Content}", response.StatusCode, content);

                    // API có thể trả về lỗi nếu kho đang có lô hàng
                    return Json(new
                    {
                        success = false,
                        message = "Không thể xóa kho. Kho có thể đang chứa lô hàng hoặc đang được sử dụng."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting warehouse");
                return Json(new
                {
                    success = false,
                    message = "Lỗi: " + ex.Message
                });
            }
        }


        // ============ UPDATE UNIT ============
        [HttpPost]
        public async Task<IActionResult> UpdateUnit([FromBody] UpdateUnitRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UnitName))
                {
                    return Json(new { success = false, message = "Tên đơn vị không được để trống" });
                }

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        unitId = request.UnitId,
                        unitName = request.UnitName,
                        unitType = (int)request.UnitType
                    }),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PutAsync($"api/Unit/{request.UnitId}", jsonContent);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Cập nhật đơn vị tính thành công"
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Lỗi từ API: {content}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật đơn vị tính");
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetBatchesByWarehouse(int warehouseId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Warehouse/warehouse/{warehouseId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var batches = JsonSerializer.Deserialize<List<BatchDTO>>(
                        content,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }
                    );

                    return Json(new
                    {
                        success = true,
                        batches = batches
                    });
                }
                else
                {
                    _logger.LogWarning("Error getting batches: {StatusCode}", response.StatusCode);
                    return Json(new
                    {
                        success = false,
                        message = "Không thể tải danh sách lô"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batches for warehouse {WarehouseId}", warehouseId);
                return Json(new
                {
                    success = false,
                    message = "Lỗi: " + ex.Message
                });
            }
        }

    }

    // ============ REQUEST MODELS ============
    public class AddUnitRequest
    {
        public string UnitName { get; set; }
        public int UnitType { get; set; }
    }

    public class AddWarehouseRequest
    {
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateWarehouseRequest
    {
        public int WarehouseId { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateUnitRequest
    {
        public int UnitId { get; set; }
        public string UnitName { get; set; }
        public UnitType UnitType { get; set; }
    }

    // ============ VIEW MODEL ============
    public class UnitWarehouseViewModel
    {
        public List<UnitDTO> Units { get; set; }
        public List<WarehouseDTO> Warehouses { get; set; }
    }

    public class DeleteWarehouseRequest
    {
        public int WarehouseId { get; set; }
    }
    public class BatchDTO
    {
        public int BatchId { get; set; }
        public string BatchCode { get; set; }
        public string MaterialName { get; set; }
        public decimal Quantity { get; set; }
        public int WarehouseId { get; set; }
    }
}