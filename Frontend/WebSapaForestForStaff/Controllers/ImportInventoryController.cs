using Microsoft.AspNetCore.Mvc;

namespace WebSapaForestForStaff.Controllers
{
    public class ImportInventoryController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Menu/ImportInventory.cshtml");
        }
    }
}
