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

        public InventoryIngredientController(IInventoryIngredientService inventoryIngredientService)
        {
            _inventoryIngredientService = inventoryIngredientService;
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

                var ingredients = await _inventoryIngredientService.GetAllIngredient();



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
    }

}
