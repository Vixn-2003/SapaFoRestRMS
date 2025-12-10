using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebSapaForestForStaff.DTOs.Customers;
using WebSapaForestForStaff.Services;

namespace WebSapaForestForStaff.Controllers
{
    [Authorize(Roles = "Manager,Owner")]
    public class ManagerCustomerController : Controller
    {
        private readonly ApiService _apiService;

        public ManagerCustomerController(ApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customers = await _apiService.GetVipCustomersAsync() ?? new List<CustomerVipListItemDto>();
            return View(customers);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var stats = await _apiService.GetCustomerVipStatisticsAsync(id);
            if (stats == null)
            {
                return NotFound();
            }

            return View(stats);
        }

        [HttpGet]
        public async Task<IActionResult> Statistics(int customerId)
        {
            var stats = await _apiService.GetCustomerVipStatisticsAsync(customerId);
            if (stats == null)
            {
                return NotFound(new { message = "Không tìm thấy khách hàng" });
            }

            return Json(stats);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateVip(int customerId, bool isVip)
        {
            var stats = await _apiService.UpdateCustomerVipAsync(customerId, isVip);
            if (stats == null)
            {
                return BadRequest(new { success = false, message = "Không thể cập nhật VIP" });
            }

            return Json(new { success = true, data = stats });
        }

        [HttpPost]
        public async Task<IActionResult> Recalculate(int customerId)
        {
            var stats = await _apiService.RecalculateCustomerVipAsync(customerId);
            if (stats == null)
            {
                return BadRequest(new { success = false, message = "Không thể tính lại VIP" });
            }

            return Json(new { success = true, data = stats });
        }
    }
}

