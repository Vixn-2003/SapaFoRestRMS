using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebSapaForestForStaff.DTOs.Inventory;

namespace WebSapaForestForStaff.Controllers
{
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
                }
                else
                {
                    _logger.LogWarning("Unit API Error: {StatusCode}", unitResponse.StatusCode);
                    ViewBag.UnitError = $"Lỗi tải dữ liệu đơn vị: {unitResponse.StatusCode}";
                }

                // ✅ Call API Warehouse
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
                }
                else
                {
                    _logger.LogWarning("Warehouse API Error: {StatusCode}", warehouseResponse.StatusCode);
                    ViewBag.WarehouseError = $"Lỗi tải dữ liệu kho: {warehouseResponse.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading unit and warehouse data");
                ViewBag.Error = $"Lỗi: {ex.Message}";
            }

            return View("~/Views/Inventory/UnitInventory.cshtml", viewModel);
        }

        // ✅ API thêm Unit
        [HttpPost]
        public async Task<IActionResult> AddUnit([FromBody] AddUnitRequest request)
        {
            try
            {
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(request),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync("api/Unit", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
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

                return Json(new
                {
                    success = false,
                    message = "Không thể thêm đơn vị"
                });
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

        // ✅ API thêm Warehouse
        [HttpPost]
        public async Task<IActionResult> AddWarehouse([FromBody] AddWarehouseRequest request)
        {
            try
            {
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(request),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync("api/Warehouse", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var warehouse = JsonSerializer.Deserialize<WarehouseDTO>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return Json(new
                    {
                        success = true,
                        message = "Thêm kho thành công",
                        warehouse = warehouse
                    });
                }

                return Json(new
                {
                    success = false,
                    message = "Không thể thêm kho"
                });
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
    }

    // DTO cho request
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

    // ViewModel
    public class UnitWarehouseViewModel
    {
        public List<UnitDTO> Units { get; set; }
        public List<WarehouseDTO> Warehouses { get; set; }
    }
}