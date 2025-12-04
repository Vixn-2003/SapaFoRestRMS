using BusinessAccessLayer.DTOs.Inventory;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using WebSapaForestForStaff.DTOs.Inventory;

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
            var viewModel = new ExportManagementViewModel();

            try
            {
                var response = await _httpClient.GetAsync("api/ExportIngredient");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var exports = JsonSerializer.Deserialize<List<StockTransactionInventoryDTO>>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    viewModel.ExportData = exports ?? new List<StockTransactionInventoryDTO>();
                }
                else
                {
                    viewModel.ErrorMessage = $"API Error: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                viewModel.ErrorMessage = $"Lỗi: {ex.Message}";
            }

            return View("~/Views/Menu/ExportManagement.cshtml", viewModel);
        }
    }

    }
