using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace SapaFoRestRMSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SupplierController : ControllerBase
    {
        private readonly IManagerSupplierService _managerSupplier;

        public SupplierController(IManagerSupplierService managerSupplier)
        {
            _managerSupplier = managerSupplier;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SupplierDTO>>> Index()
        {
            try
            {
                var supplier = await _managerSupplier.GetManagerAllSupplier();
                if (!supplier.Any())
                {
                    return NotFound("No supplier found");
                }
                return Ok(supplier);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while getting the supplier");
            }
        }
    }
}
