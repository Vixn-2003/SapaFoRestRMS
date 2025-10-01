using BusinessAccessLayer.DTOs;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebSapaForestForStaff.DTOs;

namespace WebSapaForestForStaff.Controllers
{
    public class MenuController : Controller
    {
        private readonly HttpClient _httpClient;

        public MenuController(HttpClient httpClient)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7096/");
        }

    public async Task<ActionResult> DisplayMenu()
        {
            var responseMenu = await _httpClient.GetAsync("api/Menu");
            var responseCombo = await _httpClient.GetAsync("api/Combo");

            if (responseMenu.IsSuccessStatusCode || responseCombo.IsSuccessStatusCode)
            {
                var jsonMenu = await responseMenu.Content.ReadAsStringAsync();
                var jsonCombo = await responseCombo.Content.ReadAsStringAsync();
                var productsMenu = JsonConvert.DeserializeObject<List<MenuDTO>>(jsonMenu);
                var productsCombo = JsonConvert.DeserializeObject<List<ComboDTO>>(jsonCombo);

                var vm = new MenuComboViewModel
                {
                    ProductsMenu = productsMenu ?? new(),
                    ProductsCombo = productsCombo ?? new()
                };

                return View("MenuCombo", vm);
            }
            return View("MenuCombo", new MenuComboViewModel());
        }

    }


    public class MenuComboViewModel
    {
        public List<MenuDTO> ProductsMenu { get; set; } = new();
        public List<ComboDTO> ProductsCombo { get; set; } = new();
    }

}
