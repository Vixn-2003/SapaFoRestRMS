using BusinessAccessLayer.DTOs;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebSapaForestForStaff.DTOs;

namespace WebSapaForestForStaff.Controllers
{
    public class ManagerMenuController : Controller
    {
        private readonly HttpClient _httpClient;

        public ManagerMenuController(HttpClient httpClient)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7096/");
        }

    public async Task<ActionResult> DisplayMenu()
        {
            var responseMenu = await _httpClient.GetAsync("api/ManagerMenu");
            var responseCombo = await _httpClient.GetAsync("api/ManagerCombo");

            if (responseMenu.IsSuccessStatusCode || responseCombo.IsSuccessStatusCode)
            {
                var jsonMenu = await responseMenu.Content.ReadAsStringAsync();
                var jsonCombo = await responseCombo.Content.ReadAsStringAsync();
                var productsMenu = JsonConvert.DeserializeObject<List<ManagerMenuDTO>>(jsonMenu);
                var productsCombo = JsonConvert.DeserializeObject<List<ManagerComboDTO>>(jsonCombo);

                var vm = new MenuComboViewModel
                {
                    ProductsMenu = productsMenu ?? new(),
                    ProductsCombo = productsCombo ?? new()
                };

                return View("~/Views/Menu/DashboardManager.cshtml", vm);
            }
            return View("~/Views/Menu/DashboardManager.cshtml", new MenuComboViewModel());
        }

    }


    public class MenuComboViewModel
    {
        public List<ManagerMenuDTO> ProductsMenu { get; set; } = new();
        public List<ManagerComboDTO> ProductsCombo { get; set; } = new();
    }

}
