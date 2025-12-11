using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebSapaForestForStaff.DTOs.CounterStaff;
using WebSapaForestForStaff.Services.Api.Interfaces;
using WebSapaForestForStaff.ViewModels.CounterStaff;

namespace WebSapaForestForStaff.Controllers
{
    /// <summary>
    /// MVC Controller cho Counter Staff Transaction History - UC124
    /// </summary>
    [Route("counter-staff/transactions")]
    public class CounterStaffTransactionController : Controller
    {
        private readonly ICounterTransactionApiService _transactionApiService;

        public CounterStaffTransactionController(ICounterTransactionApiService transactionApiService)
        {
            _transactionApiService = transactionApiService;
        }

        /// <summary>
        /// GET: /counter-staff/transactions
        /// Hiển thị danh sách transaction history
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(
            DateOnly? fromDate = null,
            DateOnly? toDate = null,
            string? paymentMethod = null,
            string? status = null,
            int pageNumber = 1)
        {
            var filter = new TransactionFilterDto
            {
                FromDate = fromDate ?? DateOnly.FromDateTime(DateTime.Today),
                ToDate = toDate ?? DateOnly.FromDateTime(DateTime.Today),
                PaymentMethod = paymentMethod,
                Status = status,
                PageNumber = pageNumber,
                PageSize = 20
            };

            var transactionList = await _transactionApiService.GetTransactionHistoryAsync(filter);
            if (transactionList == null)
            {
                TempData["ErrorMessage"] = "Không thể tải danh sách transactions.";
                transactionList = new TransactionHistoryListDto();
            }

            var viewModel = new TransactionHistoryViewModel
            {
                TransactionList = transactionList,
                Filter = filter
            };

            return View("~/Views/CounterStaffTransaction/Index.cshtml", viewModel);
        }

        /// <summary>
        /// POST: /counter-staff/transactions/export
        /// Export transactions to Excel
        /// </summary>
        [HttpPost("export")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportToExcel([FromForm] TransactionFilterDto filter)
        {
            try
            {
                var excelBytes = await _transactionApiService.ExportTransactionsToExcelAsync(filter);
                if (excelBytes == null || excelBytes.Length == 0)
                {
                    TempData["ErrorMessage"] = "Không thể export Excel.";
                    return RedirectToAction(nameof(Index), filter);
                }

                var fileName = $"Transactions_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi export Excel: {ex.Message}";
                return RedirectToAction(nameof(Index), filter);
            }
        }
    }
}

