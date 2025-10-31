using BusinessAccessLayer.DTOs;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Globalization;
using System.Web;
using WebSapaForestForStaff.DTOs;

namespace WebSapaForestForStaff.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayrollController : Controller
    {
        private readonly HttpClient _httpClient;

        public PayrollController()
        {
            _httpClient = new HttpClient();
        }

        [HttpGet, HttpPost]
        public async Task<IActionResult> Index(
    string? staffName = "",
    string? sortBy = "",
    bool descending = false,
    decimal? minBaseSalary = null,
    decimal? maxBaseSalary = null,
    int? minWorkDays = null,
    int? maxWorkDays = null,
    decimal? minBonus = null,
    decimal? maxBonus = null,
    decimal? minPenalty = null,
    decimal? maxPenalty = null,
    decimal? minNetSalary = null,
    decimal? maxNetSalary = null,
    string? monthYear = "",
    int pageNumber = 1,
    int pageSize = 10)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);

            if (!string.IsNullOrEmpty(staffName)) query["staffName"] = staffName;
            if (!string.IsNullOrEmpty(sortBy)) query["sortBy"] = sortBy;
            query["descending"] = descending.ToString();
            if (minBaseSalary.HasValue) query["minBaseSalary"] = minBaseSalary.Value.ToString();
            if (maxBaseSalary.HasValue) query["maxBaseSalary"] = maxBaseSalary.Value.ToString();
            if (minWorkDays.HasValue) query["minWorkDays"] = minWorkDays.Value.ToString();
            if (maxWorkDays.HasValue) query["maxWorkDays"] = maxWorkDays.Value.ToString();
            if (minBonus.HasValue) query["minBonus"] = minBonus.Value.ToString();
            if (maxBonus.HasValue) query["maxBonus"] = maxBonus.Value.ToString();
            if (minPenalty.HasValue) query["minPenalty"] = minPenalty.Value.ToString();
            if (maxPenalty.HasValue) query["maxPenalty"] = maxPenalty.Value.ToString();
            if (minNetSalary.HasValue) query["minNetSalary"] = minNetSalary.Value.ToString();
            if (maxNetSalary.HasValue) query["maxNetSalary"] = maxNetSalary.Value.ToString();
            if (!string.IsNullOrEmpty(monthYear))
            {
                var parsedDate = DateTime.ParseExact(monthYear, "yyyy-MM", CultureInfo.InvariantCulture);
                query["monthYear"] = parsedDate.ToString("MM/yyyy");
            }

            query["pageNumber"] = pageNumber.ToString();
            query["pageSize"] = pageSize.ToString();

            string apiUrl = $"https://localhost:7096/api/Payroll/getAllPayroll?{query}";

            var response = await _httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
                return View("Error");

            var jsonString = await response.Content.ReadAsStringAsync();

            var pagedResult = JsonConvert.DeserializeObject<PagedResult<PayrollDTO>>(jsonString);

            return View(pagedResult);
        }




    }
}
