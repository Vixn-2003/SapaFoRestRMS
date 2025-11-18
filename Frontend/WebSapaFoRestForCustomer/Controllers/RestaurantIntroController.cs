using Microsoft.AspNetCore.Mvc;

namespace WebSapaFoRestForCustomer.Controllers
{
    public class RestaurantIntroController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
