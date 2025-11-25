using BusinessAccessLayer.DTOs.Inventory;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;

namespace WebSapaForestForStaff.Controllers
{

        public class ExportInventoryController : Controller
        {
        private readonly HttpClient _httpClient;

        public ExportInventoryController(HttpClient httpClient)
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

                var response = await _httpClient.GetAsync("api/ExportIngredient");

                if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var exports = JsonSerializer.Deserialize<List<StockTransactionDTO>>(content, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        ViewBag.ExportData = exports;
                    }
                    else
                    {
                        ViewBag.ExportData = new List<StockTransactionDTO>();
                        ViewBag.ErrorMessage = "Không thể tải dữ liệu xuất kho";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ExportData = new List<StockTransactionDTO>();
                    ViewBag.ErrorMessage = $"Lỗi: {ex.Message}";
                }

                return View("~/Views/Menu/ExportManagement.cshtml");
            }
        }

    
}
