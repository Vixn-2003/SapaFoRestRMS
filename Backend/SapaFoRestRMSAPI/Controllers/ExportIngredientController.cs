using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExportIngredientController : ControllerBase
    {
        private readonly IStockTransactionService _transactionService;

        public ExportIngredientController(IStockTransactionService stockTransactionService)
        {
            _transactionService = stockTransactionService;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockTransactionInventoryDTO>>> ExportList()
        {
            try
            {
                var export = await _transactionService.GetAllStockExport();

                if (!export.Any())
                    return NotFound("No export found");

                return Ok(export);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Cập nhật trạng thái xuất kho
        /// </summary>

        [HttpPut("UpdateStatus")]
        public async Task<IActionResult> UpdateStatusExport([FromQuery] int transactionId, [FromQuery] string status)
        {
            try
            {
                if (transactionId <= 0)
                {
                    return BadRequest(new { message = "Transaction ID không hợp lệ" });
                }

                if (string.IsNullOrWhiteSpace(status))
                {
                    return BadRequest(new { message = "Status không được để trống" });
                }

                var result = await _transactionService.UpdateStatusExportAsync(transactionId, status);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Cập nhật trạng thái thành công",
                        transactionId = transactionId,
                        newStatus = status
                    });
                }
                else
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy giao dịch"
                    });
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi server: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Cập nhật trạng thái xuất kho (sử dụng Body)
        /// </summary>
        [HttpPut("UpdateStatus/{transactionId}")]
        public async Task<IActionResult> UpdateStatusExportBody(int transactionId, [FromBody] UpdateStatusExportRequest request)
        {
            try
            {
                if (transactionId <= 0 || transactionId != request.TransactionId)
                {
                    return BadRequest(new { message = "Transaction ID không hợp lệ" });
                }

                var result = await _transactionService.UpdateStatusExportAsync(request.TransactionId, request.StatusExport);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Cập nhật trạng thái thành công",
                        transactionId = request.TransactionId,
                        newStatus = request.StatusExport
                    });
                }
                else
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy giao dịch"
                    });
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi server: " + ex.Message
                });
            }
        }
    }
}
