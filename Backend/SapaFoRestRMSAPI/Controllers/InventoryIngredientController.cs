using BusinessAccessLayer.DTOs.Filter;
using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryIngredientController : Controller
    {

        private readonly IInventoryIngredientService _inventoryIngredientService;
        private readonly IWarehouseService _warehouseService;

        public InventoryIngredientController(IInventoryIngredientService inventoryIngredientService, IWarehouseService warehouseService)
        {
            _inventoryIngredientService = inventoryIngredientService;
            _warehouseService = warehouseService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryIngredientDTO>>> DisplayIngredents()
        {
            try
            {
                var ingredients = await _inventoryIngredientService.GetAllIngredient();

                foreach (var ingredient in ingredients)
                {
                    decimal TE = 0;
                    decimal TI = 0;
                    decimal TFirst = 0;
                    foreach (var b in ingredient.Batches)
                    {
                        var totalIE = await _inventoryIngredientService.GetImportExportBatchesId(
                            b.BatchId, DateTime.Now.AddDays(-7), DateTime.Now
                        );
                        TE += totalIE.TExport;
                        TI += totalIE.TImport;
                        TFirst = totalIE.totalFirst;

                    }
                    ingredient.TotalImport = TI;
                    ingredient.TotalExport = TE;
                    ingredient.OriginalQuantity = TFirst;
                }

                if (!ingredients.Any())
                    return NotFound("No ingredient found");

                return Ok(ingredients);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("filter")]
        public async Task<ActionResult<IEnumerable<InventoryIngredientDTO>>> FilterIngredents([FromBody] IngredientFilterRequest? request)
        {
            try
            {
                var fromDate = request?.FromDate ?? DateTime.Now.AddDays(-7);
                var toDate = request?.ToDate ?? DateTime.Now;
                var search = request?.SearchIngredent;
                IEnumerable<InventoryIngredientDTO> ingredients;
                if (string.IsNullOrEmpty(search))
                {                   
                    ingredients = await _inventoryIngredientService.GetAllIngredient();
                }
                else
                {
                    ingredients = await _inventoryIngredientService.GetAllIngredientSearch(search);
                }


                foreach (var ingredient in ingredients)
                {
                    decimal TE = 0;
                    decimal TI = 0;
                    decimal TFirst = 0;
                    foreach (var b in ingredient.Batches)
                    {
                        var totalIE = await _inventoryIngredientService.GetImportExportBatchesId(
                            b.BatchId, fromDate, toDate
                        );
                        TE += totalIE.TExport;
                        TI += totalIE.TImport;
                        TFirst = totalIE.totalFirst;

                    }
                    ingredient.TotalImport = TI;
                    ingredient.TotalExport = TE;
                    ingredient.OriginalQuantity = TFirst;
                }

                if (!ingredients.Any())
                    return NotFound("No ingredient found");

                return Ok(ingredients);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("BatchIngredient/{id}")]
        public async Task<ActionResult<List<BatchIngredientDTO>>> GetBatchIngredient(int id)
        {
            try
            {
                var batches = await _inventoryIngredientService.GetBatchesAsync(id);

                var result = batches.ToList();

                if (result == null || result.Count == 0)
                {
                    return Ok(new List<BatchIngredientDTO>());
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error in GetBatchIngredient: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                return StatusCode(500, new
                {
                    message = "Có lỗi xảy ra khi lấy dữ liệu",
                    error = ex.Message
                });
            }
        }

        [HttpPut("UpdateBatchWarehouse")]
        public async Task<IActionResult> UpdateBatchWarehouse([FromBody] UpdateBatchWarehouseRequest request)
        {
            try
            {
               // _logger.LogInformation($"Nhận request cập nhật kho: BatchId={request.BatchId}, WarehouseId={request.WarehouseId}");

                // Validate request
                if (request.BatchId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "BatchId không hợp lệ"
                    });
                }

                if (request.WarehouseId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "WarehouseId không hợp lệ"
                    });
                }

                // Kiểm tra warehouse có tồn tại và active không
                var warehouseExists = await _warehouseService.GetWarehouseById(request.WarehouseId);
                if (warehouseExists == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Không tìm thấy kho #{request.WarehouseId}"
                    });
                }

                // Cập nhật warehouse cho batch
                var result = await _inventoryIngredientService.UpdateBatchWarehouse(request.BatchId, request.WarehouseId);

                if (result)
                {
                   // _logger.LogInformation($"Cập nhật kho thành công: BatchId={request.BatchId}, WarehouseId={request.WarehouseId}");

                    return Ok(new
                    {
                        success = true,
                        message = "Cập nhật kho thành công",
                        data = new
                        {
                            batchId = request.BatchId,
                            warehouseId = request.WarehouseId
                        }
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Không thể cập nhật kho"
                    });
                }
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex, $"Lỗi khi cập nhật kho cho batch {request.BatchId}");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi cập nhật kho",
                    error = ex.Message
                });
            }
        }

        public class UpdateBatchWarehouseRequest
        {
            public int BatchId { get; set; }
            public int WarehouseId { get; set; }
        }

        [HttpPut("UpdateIngredient")]
        public async Task<IActionResult> UpdateIngredient([FromBody] UpdateIngredientRequest request)
        {
            try
            {
                // Validate request
                if (request.IngredientId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "IngredientId không hợp lệ"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Tên nguyên liệu không được để trống"
                    });
                }

                // Gọi service để cập nhật
                var (success, message) = await _inventoryIngredientService.UpdateIngredient(
                    request.IngredientId,
                    request.Name.Trim(),
                    request.UnitId
                );

                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = message,
                        data = new
                        {
                            ingredientId = request.IngredientId,
                            name = request.Name.Trim(),
                            unit = request.UnitId
                        }
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = message
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateIngredient API: {ex.Message}");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi cập nhật nguyên liệu",
                    error = ex.Message
                });
            }
        }
    }

}
