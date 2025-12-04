using Microsoft.AspNetCore.Mvc;

namespace WebSapaForestForStaff.Controllers
{
    public class DashboardInventoryController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Inventory/DashboardInventory.cshtml");
        }
    }
}
