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

        

        
    }
}
