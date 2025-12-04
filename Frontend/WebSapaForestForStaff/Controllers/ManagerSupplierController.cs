using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace WebSapaForestForStaff.Controllers
{
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
    }
}