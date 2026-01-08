using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;
using WebSapaForestForStaff.DTOs;
using WebSapaForestForStaff.DTOs.Inventory;
using Microsoft.AspNetCore.Authorization;

namespace WebSapaForestForStaff.Controllers
{
    [Authorize(Policy = "Position:Inventory")]
    public class ManagerSupplierController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ManagerSupplierController> _logger;

        public ManagerSupplierController(IHttpClientFactory httpClientFactory, ILogger<ManagerSupplierController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // Route: GET /ManagerSupplier (View trang chính)
        [HttpGet]
        [Route("ManagerSupplier")]
        public IActionResult Index()
        {
            return View("~/Views/Inventory/ManagerSupplier.cshtml");
        }

        // Route: GET /ManagerSupplier/Summary (API cho AJAX)
        [HttpGet]
        [Route("ManagerSupplier/Summary")]
        public async Task<IActionResult> GetSuppliersSummary()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("API");
                _logger.LogInformation($"BaseAddress: {httpClient.BaseAddress}");
                _logger.LogInformation("Calling API: api/inventory/Supplier/summary-list");

                var response = await httpClient.GetAsync("api/inventory/Supplier/summary-list");

                _logger.LogInformation($"Response Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API Error: {errorContent}");

                    return StatusCode((int)response.StatusCode, new
                    {
                        message = "Lỗi khi tải dữ liệu tổng hợp nhà cung cấp.",
                        details = errorContent
                    });
                }

                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: {ex.Message}");
                _logger.LogError($"StackTrace: {ex.StackTrace}");

                return StatusCode(500, new
                {
                    message = "Lỗi không xác định.",
                    error = ex.Message
                });
            }
        }

        // Route: GET /ManagerSupplier/OrdersHistory/{id}
        [HttpGet]
        [Route("ManagerSupplier/OrdersHistory/{id}")]
        public async Task<IActionResult> GetOrdersHistory(int id)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("API");
                _logger.LogInformation($"Calling API: api/inventory/Supplier/{id}/orders-history");

                var response = await httpClient.GetAsync($"api/inventory/Supplier/{id}/orders-history");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API Error: {errorContent}");

                    return StatusCode((int)response.StatusCode, new
                    {
                        message = "Lỗi khi tải lịch sử đơn hàng.",
                        details = errorContent
                    });
                }

                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: {ex.Message}");
                return StatusCode(500, new
                {
                    message = "Lỗi không xác định.",
                    error = ex.Message
                });
            }
        }

        // Route: GET /ManagerSupplier/ProductsSupplied/{id}
        [HttpGet]
        [Route("ManagerSupplier/ProductsSupplied/{id}")]
        public async Task<IActionResult> GetProductsSupplied(int id)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("API");
                _logger.LogInformation($"Calling API: api/inventory/Supplier/{id}/products-supplied");

                var response = await httpClient.GetAsync($"api/inventory/Supplier/{id}/products-supplied");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API Error: {errorContent}");

                    return StatusCode((int)response.StatusCode, new
                    {
                        message = "Lỗi khi tải danh mục sản phẩm cung cấp.",
                        details = errorContent
                    });
                }

                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: {ex.Message}");
                return StatusCode(500, new
                {
                    message = "Lỗi không xác định.",
                    error = ex.Message
                });
            }
        }
        // Route: DELETE /ManagerSupplier/Delete/{id}
        [HttpDelete]
        [Route("ManagerSupplier/Delete/{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("API");
                _logger.LogInformation($"Calling API: api/inventory/Supplier/{id}");

                var response = await httpClient.DeleteAsync($"api/inventory/Supplier/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API Error: {errorContent}");
                    return StatusCode((int)response.StatusCode, new { message = "Lỗi khi xóa nhà cung cấp." });
                }

                return Ok(new { message = "Xóa nhà cung cấp thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi không xác định." });
            }
        }
        // Route: GET /ManagerSupplier/RecentTransactions
        [HttpGet]
        [Route("ManagerSupplier/RecentTransactions")]
        public async Task<IActionResult> GetRecentTransactions()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("API");
                _logger.LogInformation("Calling API: api/PurchaseOrder");

                var response = await httpClient.GetAsync("api/PurchaseOrder");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API Error: {errorContent}");
                    return StatusCode((int)response.StatusCode, new { message = "Lỗi khi tải giao dịch gần đây." });
                }

                var jsonData = await response.Content.ReadAsStringAsync();
                var allOrders = JsonConvert.DeserializeObject<List<PurchaseOrderDTO>>(jsonData);

                // Lọc chỉ đơn đã hoàn thành và sắp xếp theo TimeConfirm giảm dần
                var recentOrders = allOrders
                    .Where(po => po.Status == "Completed" && po.TimeConfirm.HasValue)
                    .OrderByDescending(po => po.TimeConfirm)
                    .Take(5) // Lấy 5 đơn gần nhất
                    .Select(po => new
                    {
                        supplierName = po.Supplier?.Name ?? "N/A",
                        totalValue = po.PurchaseOrderDetails?.Sum(d => d.Subtotal) ?? 0,
                        timeConfirm = po.TimeConfirm
                    })
                    .ToList();

                return Ok(recentOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi không xác định.", error = ex.Message });
            }
        }


        [HttpGet]
        [Route("ManagerSupplier/AllRecentTransactions")]
        public async Task<IActionResult> GetAllRecentTransactions()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("API");
                _logger.LogInformation("Calling API: api/PurchaseOrder");

                var response = await httpClient.GetAsync("api/PurchaseOrder");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API Error: {errorContent}");
                    return StatusCode((int)response.StatusCode, new { message = "Lỗi khi tải giao dịch." });
                }

                var jsonData = await response.Content.ReadAsStringAsync();
                var allOrders = JsonConvert.DeserializeObject<List<PurchaseOrderDTO>>(jsonData);

                // Lọc 50 đơn hoàn thành gần nhất
                var recentOrders = allOrders
                    .Where(po => po.Status == "Completed" && po.TimeConfirm.HasValue)
                    .OrderByDescending(po => po.TimeConfirm)
                    .Take(50)
                    .Select(po => new
                    {
                        supplierName = po.Supplier?.Name ?? "N/A",
                        supplierCode = po.Supplier?.CodeSupplier ?? "N/A",
                        totalValue = po.PurchaseOrderDetails?.Sum(d => d.Subtotal) ?? 0,
                        timeConfirm = po.TimeConfirm
                    })
                    .ToList();

                return Ok(recentOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi không xác định.", error = ex.Message });
            }
        }

        // Route: GET /ManagerSupplier/TopSuppliers
        [HttpGet]
        [Route("ManagerSupplier/TopSuppliers")]
        public async Task<IActionResult> GetTopSuppliers()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("API");

                // Lấy danh sách suppliers
                var suppliersResponse = await httpClient.GetAsync("api/inventory/Supplier/summary-list");
                if (!suppliersResponse.IsSuccessStatusCode)
                {
                    return StatusCode((int)suppliersResponse.StatusCode, new { message = "Lỗi khi tải danh sách nhà cung cấp." });
                }

                var suppliersJson = await suppliersResponse.Content.ReadAsStringAsync();
                var suppliers = JsonConvert.DeserializeObject<List<SupplierListDto>>(suppliersJson);

                // Sắp xếp theo TotalValue và lấy top 5
                var topSuppliers = suppliers
                    .OrderByDescending(s => s.TotalValue)
                    .Take(5)
                    .Select(s => new
                    {
                        name = s.Name,
                        code = s.Code,
                        value = s.TotalValue,
                        orders = s.TotalOrders
                    })
                    .ToList();

                return Ok(topSuppliers);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi không xác định.", error = ex.Message });
            }
        }

        // Route: GET /ManagerSupplier/MonthlyStats
        [HttpGet]
        [Route("ManagerSupplier/MonthlyStats")]
        public async Task<IActionResult> GetMonthlyStats()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("API");
                _logger.LogInformation("Calling API: api/PurchaseOrder");

                var response = await httpClient.GetAsync("api/PurchaseOrder");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API Error: {errorContent}");
                    return StatusCode((int)response.StatusCode, new { message = "Lỗi khi tải dữ liệu." });
                }

                var jsonData = await response.Content.ReadAsStringAsync();
                var allOrders = JsonConvert.DeserializeObject<List<PurchaseOrderDTO>>(jsonData);

                // Lấy tháng hiện tại
                var now = DateTime.Now;
                var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                // Đếm số đơn hoàn thành trong tháng này
                var ordersThisMonth = allOrders
                    .Where(po => po.Status == "Completed"
                              && po.TimeConfirm.HasValue
                              && po.TimeConfirm.Value >= firstDayOfMonth
                              && po.TimeConfirm.Value <= lastDayOfMonth)
                    .Count();

                // Tính tổng giá trị đơn hàng tháng này
                var valueThisMonth = allOrders
                    .Where(po => po.Status == "Completed"
                              && po.TimeConfirm.HasValue
                              && po.TimeConfirm.Value >= firstDayOfMonth
                              && po.TimeConfirm.Value <= lastDayOfMonth)
                    .Sum(po => po.PurchaseOrderDetails?.Sum(d => d.Subtotal) ?? 0);

                return Ok(new
                {
                    ordersThisMonth = ordersThisMonth,
                    valueThisMonth = valueThisMonth,
                    month = now.Month,
                    year = now.Year
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi không xác định.", error = ex.Message });
            }
        }

        [HttpGet]
        [Route("ManagerSupplier/OrderDetails/{orderId}")]
        public async Task<IActionResult> GetOrderDetails(string orderId)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("API");
                var response = await httpClient.GetAsync($"api/PurchaseOrder/Detail/{orderId}");

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new { message = "Lỗi khi tải chi tiết đơn hàng." });
                }

                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi không xác định." });
            }
        }

        // Route: POST /ManagerSupplier/Create
        [HttpPost]
        [Route("ManagerSupplier/Create")]
        public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierDTO dto)
        {
            try
            {
                // Validate
                if (string.IsNullOrWhiteSpace(dto.CodeSupplier) ||
                    string.IsNullOrWhiteSpace(dto.Name) ||
                    string.IsNullOrWhiteSpace(dto.ContactInfo) ||
                    string.IsNullOrWhiteSpace(dto.Phone) ||
                    string.IsNullOrWhiteSpace(dto.Email) ||
                    string.IsNullOrWhiteSpace(dto.Address))
                {
                    return BadRequest(new { message = "Tất cả các trường không được để trống." });
                }

                var httpClient = _httpClientFactory.CreateClient("API");

                // Kiểm tra trùng mã
                var checkResponse = await httpClient.GetAsync($"api/inventory/Supplier/check-code/{dto.CodeSupplier}");
                if (checkResponse.IsSuccessStatusCode)
                {
                    var exists = await checkResponse.Content.ReadAsStringAsync();
                    if (exists == "true")
                    {
                        return BadRequest(new { message = "Mã nhà cung cấp đã tồn tại." });
                    }
                }

                var response = await httpClient.PostAsJsonAsync("api/inventory/Supplier", dto);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API Error: {errorContent}");
                    return StatusCode((int)response.StatusCode, new { message = "Lỗi khi tạo nhà cung cấp." });
                }

                return Ok(new { message = "Tạo nhà cung cấp thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi không xác định." });
            }
        }

        // Route: PUT /ManagerSupplier/Update/{id}
        [HttpPut]
        [Route("ManagerSupplier/Update/{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] UpdateSupplierDTO dto)
        {
            try
            {
                // Validate
                if (string.IsNullOrWhiteSpace(dto.Name) ||
                    string.IsNullOrWhiteSpace(dto.ContactInfo) ||
                    string.IsNullOrWhiteSpace(dto.Phone) ||
                    string.IsNullOrWhiteSpace(dto.Email) ||
                    string.IsNullOrWhiteSpace(dto.Address))
                {
                    return BadRequest(new { message = "Tất cả các trường không được để trống." });
                }

                var httpClient = _httpClientFactory.CreateClient("API");
                var response = await httpClient.PutAsJsonAsync($"api/inventory/Supplier/{id}", dto);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API Error: {errorContent}");
                    return StatusCode((int)response.StatusCode, new { message = "Lỗi khi cập nhật nhà cung cấp." });
                }

                return Ok(new { message = "Cập nhật nhà cung cấp thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi không xác định." });
            }
        }

    }
}