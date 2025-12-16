using BusinessAccessLayer.DTOs.CounterStaff;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SapaFoRestRMSAPI.Controllers
{
    /// <summary>
    /// API Controller cho Counter Transaction History - UC124
    /// Counter Staff: View transaction history + Export Excel
    /// </summary>
    [ApiController]
    [Route("api/counter/transactions")]
    [Authorize(Roles = "Owner,Manager,Staff")]
    public class CounterTransactionController : ControllerBase
    {
        private readonly ICounterTransactionService _transactionService;

        public CounterTransactionController(ICounterTransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        /// <summary>
        /// POST: /api/counter/transactions/filter
        /// Lấy danh sách transaction history với filter và phân trang
        /// </summary>
        [HttpPost("filter")]
        public async Task<IActionResult> GetTransactionHistory(
            [FromBody] TransactionFilterDto filter,
            CancellationToken ct = default)
        {
            try
            {
                var result = await _transactionService.GetTransactionHistoryAsync(filter, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách transactions", error = ex.Message });
            }
        }

        /// <summary>
        /// POST: /api/counter/transactions/export-excel
        /// Export transactions to Excel file
        /// </summary>
        [HttpPost("export-excel")]
        public async Task<IActionResult> ExportToExcel(
            [FromBody] TransactionFilterDto filter,
            CancellationToken ct = default)
        {
            try
            {
                var excelBytes = await _transactionService.ExportTransactionsToExcelAsync(filter, ct);
                
                var fileName = $"Transactions_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi export Excel", error = ex.Message });
            }
        }
    }
}

