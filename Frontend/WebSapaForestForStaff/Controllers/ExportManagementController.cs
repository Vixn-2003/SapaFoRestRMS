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
        private readonly IConfiguration _configuration;

        public ExportManagementController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            // Set API base URL from appsettings.json
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7096/api";
            ViewBag.ApiBaseUrl = apiBaseUrl;
           
            return View("~/Views/Menu/ExportManagement.cshtml");
        }
    }

}
