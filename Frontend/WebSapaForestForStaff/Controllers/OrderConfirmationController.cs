using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WebSapaForestForStaff.Controllers
{
    /// <summary>
    /// Controller cho màn hình xác nhận hóa đơn
    /// </summary>
    [Authorize(Roles = "Staff,Manager,Admin")]
    public class OrderConfirmationController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OrderConfirmationController> _logger;
        private readonly IConfiguration _configuration;
        
        public OrderConfirmationController(
            IHttpClientFactory httpClientFactory,
            ILogger<OrderConfirmationController> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }
        
        /// <summary>
        /// GET: Màn hình xác nhận hóa đơn
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int orderId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("API");
                var token = Request.Cookies["jwtToken"];
                
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
                
                var response = await client.GetAsync($"api/orderconfirmation/{orderId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                        return RedirectToAction("Index", "CashierPaymentFlow");
                    }
                    
                    TempData["ErrorMessage"] = "Không thể tải thông tin đơn hàng";
                    return RedirectToAction("Index", "CashierPaymentFlow");
                }
                
                var json = await response.Content.ReadAsStringAsync();
                var order = JsonSerializer.Deserialize<dynamic>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                ViewBag.Order = order;
                ViewBag.OrderId = orderId;
                
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order confirmation for OrderId: {OrderId}", orderId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải đơn hàng";
                return RedirectToAction("Index", "CashierPaymentFlow");
            }
        }
        
        /// <summary>
        /// POST: Xác nhận hóa đơn
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm([FromForm] int orderId, [FromForm] string consumptionItemsJson, [FromForm] string? notes)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("API");
                var token = Request.Cookies["jwtToken"];
                
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
                
                // Parse consumption items
                var consumptionItems = string.IsNullOrEmpty(consumptionItemsJson) 
                    ? new List<object>() 
                    : JsonSerializer.Deserialize<List<object>>(consumptionItemsJson) ?? new List<object>();
                
                var request = new
                {
                    orderId = orderId,
                    consumptionItems = consumptionItems,
                    notes = notes,
                    confirmedByStaffId = GetCurrentStaffId()
                };
                
                var jsonContent = JsonSerializer.Serialize(request);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var response = await client.PostAsync("api/orderconfirmation/confirm", httpContent);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Xác nhận hóa đơn thành công!";
                    return RedirectToAction("Payment", "CashierPaymentFlow", new { id = orderId });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Không thể xác nhận đơn hàng: {errorContent}";
                    return RedirectToAction("Index", new { orderId = orderId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming order {OrderId}", orderId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xác nhận đơn hàng";
                return RedirectToAction("Index", new { orderId = orderId });
            }
        }
        
        private int? GetCurrentStaffId()
        {
            var staffIdClaim = User.FindFirst("StaffId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (int.TryParse(staffIdClaim, out var staffId))
            {
                return staffId;
            }
            
            return null;
        }
    }
}

