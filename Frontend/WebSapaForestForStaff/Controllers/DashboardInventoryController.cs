using BusinessAccessLayer.DTOs.Inventory;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebSapaForestForStaff.DTOs.Inventory;
using WebSapaForestForStaff.DTOs;

public class DashboardInventoryController : Controller
{
    private readonly HttpClient _httpClient;

    public DashboardInventoryController(HttpClient httpClient)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7096/")
        };
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new DashboardViewModel
        {
            Ingredients = new List<InventoryIngredientDTO>(),
            PurchaseOrders = new List<PurchaseOrderDTO>(),
            AuditInventories = new List<AuditInventoryDTO>()
        };

        try
        {
            // ✅ CẤU HÌNH JSONSERIALIZERSETTINGS
            var jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new DefaultNamingStrategy() // Giữ nguyên PascalCase
                },
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            // 1. Ingredients
            var ingredientResponse = await _httpClient.GetAsync("api/InventoryIngredient");
            if (ingredientResponse.IsSuccessStatusCode)
            {
                var json = await ingredientResponse.Content.ReadAsStringAsync();
                viewModel.Ingredients = JsonConvert.DeserializeObject<List<InventoryIngredientDTO>>(json, jsonSettings)
                                      ?? new List<InventoryIngredientDTO>();
            }

            // 2. Purchase Orders
            var purchaseResponse = await _httpClient.GetAsync("api/PurchaseOrder");
            if (purchaseResponse.IsSuccessStatusCode)
            {
                var json = await purchaseResponse.Content.ReadAsStringAsync();
                viewModel.PurchaseOrders = JsonConvert.DeserializeObject<List<PurchaseOrderDTO>>(json, jsonSettings)
                                         ?? new List<PurchaseOrderDTO>();
            }

            // 3. Audit Inventories
            var auditResponse = await _httpClient.GetAsync("api/AuditInventory/GetAll");
            if (auditResponse.IsSuccessStatusCode)
            {
                var json = await auditResponse.Content.ReadAsStringAsync();
                viewModel.AuditInventories = JsonConvert.DeserializeObject<List<AuditInventoryDTO>>(json, jsonSettings)
                                           ?? new List<AuditInventoryDTO>();
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
        }

        return View("~/Views/Inventory/DashboardInventory.cshtml", viewModel);
    }
}