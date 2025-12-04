using Microsoft.AspNetCore.Mvc;

namespace WebSapaForestForStaff.Controllers
{
    public class UnitInventoryController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Inventory/UnitInventory.cshtml");
        }
    }
}
