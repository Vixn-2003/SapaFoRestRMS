using Microsoft.AspNetCore.Mvc;

namespace WebSapaFoRestForCustomer.Controllers
{
    public class MomoTestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
