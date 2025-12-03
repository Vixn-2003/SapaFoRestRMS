using Microsoft.AspNetCore.Mvc;

namespace WebSapaForestForStaff.Controllers
{
    public class HomeManagerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
