using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseOrderDetailController : ControllerBase
    {
        private readonly IPurchaseOrderDetailService _purchaseOrderDetailService;

        public PurchaseOrderDetailController(IPurchaseOrderDetailService purchaseOrderDetailService)
        {
            _purchaseOrderDetailService = purchaseOrderDetailService;
        }

        /// <summary>
        /// Lấy danh sách PurchaseOrderDetail theo IngredientId kèm thông tin Supplier
        /// </summary>
        /// <param name="ingredientId">ID của nguyên liệu</param>
        /// <returns>Danh sách chi tiết đơn mua hàng</returns>
        [HttpGet("GetByIngredient/{ingredientId}")]
        public async Task<IActionResult> GetByIngredient(int ingredientId)
        {
            try
            {
                var result = await _purchaseOrderDetailService.GetByIngredientIdAsync(ingredientId);

                if (result == null || !result.Any())
                {
                    return NotFound(new { message = "Không tìm thấy lịch sử giao dịch cho nguyên liệu này" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }

        /// <summary>
        /// So sánh nhà cung cấp theo tiêu chí (giá hoặc số lượng)
        /// </summary>
        /// <param name="ingredientId">ID của nguyên liệu</param>
        /// <param name="compareBy">Tiêu chí so sánh: "price" hoặc "quantity"</param>
        /// <returns>Danh sách nhà cung cấp đã sắp xếp</returns>
        [HttpGet("CompareSuppliers/{ingredientId}")]
        public async Task<IActionResult> CompareSuppliers(int ingredientId, [FromQuery] string compareBy = "price")
        {
            try
            {
                if (compareBy.ToLower() != "price" && compareBy.ToLower() != "quantity")
                {
                    return BadRequest(new { message = "compareBy phải là 'price' hoặc 'quantity'" });
                }

                var result = await _purchaseOrderDetailService.GetSupplierComparisonAsync(ingredientId, compareBy);

                if (result == null || !result.Any())
                {
                    return NotFound(new { message = "Không tìm thấy nhà cung cấp nào từng cung cấp nguyên liệu này" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }
    }
}
