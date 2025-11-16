using BusinessAccessLayer.DTOs.Inventory;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebSapaForestForStaff.DTOs;

namespace WebSapaForestForStaff.Controllers
{
    public class MainImportInventoryController : Controller
    {
        private readonly HttpClient _httpClient;

        public MainImportInventoryController(HttpClient httpClient)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:7096/")
            };
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Gọi API với endpoint đúng
                var response = await _httpClient.GetAsync("api/PurchaseOrder");

                // Kiểm tra status code
                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage = $"Lỗi API: {response.StatusCode}";
                    return View("~/Views/Menu/MainImportInventory.cshtml", new List<PurchaseOrderDTO>());
                }

                // Đọc content
                var jsonData = await response.Content.ReadAsStringAsync();

                // Debug - log ra để kiểm tra
                Console.WriteLine("API Response: " + jsonData);

                // Parse JSON
                var purchaseList = JsonConvert.DeserializeObject<List<PurchaseOrderDTO>>(jsonData);

                // Kiểm tra null
                if (purchaseList == null)
                {
                    purchaseList = new List<PurchaseOrderDTO>();
                }

                return View("~/Views/Menu/MainImportInventory.cshtml", purchaseList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                ViewBag.ErrorMessage = ex.Message;
                return View("~/Views/Menu/MainImportInventory.cshtml", new List<PurchaseOrderDTO>());
            }
        }

        
    }
}
