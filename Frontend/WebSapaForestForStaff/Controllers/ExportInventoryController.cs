using Microsoft.AspNetCore.Mvc;

namespace WebSapaForestForStaff.Controllers
{
    public class ExportInventoryController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Menu/ExportInventory.cshtml");
        }
    }
}
