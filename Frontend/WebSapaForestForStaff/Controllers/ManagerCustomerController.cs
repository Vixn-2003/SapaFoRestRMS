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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateVip(int customerId, bool isVip)
        {
            var stats = await _apiService.UpdateCustomerVipAsync(customerId, isVip);
            if (stats == null)
            {
                TempData["ErrorMessage"] = "Không thể cập nhật trạng thái VIP. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái VIP thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Recalculate(int customerId)
        {
            var stats = await _apiService.RecalculateCustomerVipAsync(customerId);
            if (stats == null)
            {
                TempData["ErrorMessage"] = "Không thể tính lại VIP. Vui lòng thử lại.";
                return RedirectToAction(nameof(Details), new { id = customerId });
            }

            TempData["SuccessMessage"] = "Đã tính lại trạng thái VIP thành công!";
            return RedirectToAction(nameof(Details), new { id = customerId });
        }
    }
}

