using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebSapaForestForStaff.Services.Api.Interfaces;
using WebSapaForestForStaff.ViewModels.CounterStaff;

namespace WebSapaForestForStaff.Controllers
{
    /// <summary>
    /// MVC Controller cho Counter Staff Order List - UC123
    /// Reuse OrderSelection.cshtml
    /// </summary>
    /// 
    [Authorize(Policy = "Position:Cashier")]

    [Route("counter-staff/orders")]
    public class CounterStaffOrderController : Controller
    {
        private readonly ICounterStaffOrderApiService _orderApiService;

        public CounterStaffOrderController(ICounterStaffOrderApiService orderApiService)
        {
            _orderApiService = orderApiService;
        }

        /// <summary>
        /// GET: /counter-staff/orders
        /// Hiển thị danh sách orders
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(
            DateOnly? date = null,
            string? status = null,
            string? searchKeyword = null)
        {
            var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Today);
            var selectedStatus = status ?? "Confirmed";

            var orders = await _orderApiService.GetOrdersAsync(selectedStatus, selectedDate, null, searchKeyword);
            if (orders == null)
            {
                TempData["ErrorMessage"] = "Không thể tải danh sách orders.";
                orders = new System.Collections.Generic.List<DTOs.CounterStaff.OrderListItemDto>();
            }

            var viewModel = new OrderListViewModel
            {
                Orders = orders,
                SelectedDate = selectedDate,
                SelectedStatus = selectedStatus,
                SearchKeyword = searchKeyword
            };

            // ✅ Reuse OrderSelection.cshtml (đã có sẵn)
            // Cần update OrderSelection.cshtml để hỗ trợ filter bar và notice bar
            return View("~/Views/CashierFlow/OrderSelection.cshtml", viewModel);
        }
    }
}

