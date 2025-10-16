using Microsoft.AspNetCore.Mvc;

namespace WebSapaForestForStaff.Controllers
{
    public class MainImportInventoryController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Menu/MainImportInventory.cshtml");
        }
    }
}
