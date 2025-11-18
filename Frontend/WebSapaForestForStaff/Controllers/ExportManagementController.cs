using BusinessAccessLayer.DTOs.Inventory;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using WebSapaForestForStaff.DTOs.Inventory;
using WebSapaForestForStaff.DTOs;

namespace WebSapaForestForStaff.Controllers
{
    public class ExportManagementController : Controller
    {
        public async Task<IActionResult> Index()
        {
           
            return View("~/Views/Menu/ExportManagement.cshtml");
        }
    }

}
