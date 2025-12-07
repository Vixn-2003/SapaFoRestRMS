using BusinessAccessLayer.DTOs.Inventory;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using WebSapaForestForStaff.DTOs.Inventory;
using WebSapaForestForStaff.DTOs;
using System.Text.Json;

namespace WebSapaForestForStaff.Controllers
{
    public class ExportManagementController : Controller
    {
        private readonly HttpClient _httpClient;

        public ExportManagementController(HttpClient httpClient)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:7096/")
            };
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new ExportManagementViewModel();

            try
            {
                var response = await _httpClient.GetAsync("api/ExportIngredient");

                // Log để debug
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response: {content}");

                if (response.IsSuccessStatusCode)
                {
                    var exports = JsonSerializer.Deserialize<List<StockTransactionInventoryDTO>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Console.WriteLine($"Deserialized: {exports?.Count ?? 0} items");

                    viewModel.ExportData = exports ?? new List<StockTransactionInventoryDTO>();
                }
                else
                {
                    Console.WriteLine($"API Error: {response.StatusCode}");
                    viewModel.ExportData = new List<StockTransactionInventoryDTO>();
                    viewModel.ErrorMessage = $"API Error: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                viewModel.ExportData = new List<StockTransactionInventoryDTO>();
                viewModel.ErrorMessage = $"Lỗi: {ex.Message}";
            }

            return View("~/Views/Menu/ExportManagement.cshtml", viewModel);
        }
    }

}
